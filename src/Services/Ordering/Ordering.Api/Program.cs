using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Ordering.Application;
using Ordering.Domain;
using Ordering.Domain.Common;
using Ordering.Infrastructure;
using Ordering.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("La chaîne de connexion DefaultConnection est requise.");

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>();

var catalogApiBaseUrl = builder.Configuration["Services:CatalogApi"]
    ?? throw new InvalidOperationException("La configuration Services:CatalogApi est requise.");

builder.Services.AddHttpClient<IProductCatalogClient, HttpProductCatalogClient>(client =>
{
    client.BaseAddress = new Uri(catalogApiBaseUrl);
});

builder.Services.AddHttpClient<IStockReservationClient, HttpStockReservationClient>(client =>
{
    client.BaseAddress = new Uri(catalogApiBaseUrl);
});

builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, CheckoutService>();

builder.Services.AddOptions<StripeOptions>().Bind(builder.Configuration.GetSection(StripeOptions.SectionName));
builder.Services.AddScoped<IPaymentGateway, StripePaymentGateway>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// Même clé de signature que Identity.Api — c'est ce qui permet à ce service de valider
// l'identité d'un appelant sans dépendre directement d'Identity.Api à l'exécution.
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("Jwt:SecretKey est requis — doit être identique sur Identity.Api, Catalog.Api et Ordering.Api.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "Ecommerce";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "Ecommerce";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            ValidateLifetime = true,
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Gestionnaire global : traduit toute DomainException (violation d'une règle métier) en Problem
// Details 400 avec le message du domaine, au lieu de laisser chaque endpoint répéter son propre
// try/catch. Toute autre exception est logguée (jamais avalée silencieusement) puis renvoyée
// comme Problem Details 500 générique, sans fuite de détails internes au client.
app.UseExceptionHandler(exceptionApp => exceptionApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

    if (exception is DomainException domainException)
    {
        await Results.Problem(title: "Règle métier violée", detail: domainException.Message, statusCode: StatusCodes.Status400BadRequest)
            .ExecuteAsync(context);
        return;
    }

    context.RequestServices.GetRequiredService<ILogger<Program>>()
        .LogError(exception, "Erreur non gérée lors du traitement de {Method} {Path}", context.Request.Method, context.Request.Path);

    await Results.Problem(title: "Une erreur inattendue est survenue.", statusCode: StatusCodes.Status500InternalServerError)
        .ExecuteAsync(context);
}));

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

// Le panier et le checkout restent accessibles par customerId de route (panier anonyme, cookie
// côté Web - voir Issue #6) : un visiteur non connecté doit pouvoir acheter. Seule la consultation
// de l'historique (ci-dessous) exige une identité authentifiée, puisqu'un client sans compte n'a
// justement rien à "retrouver" d'une visite à l'autre.
app.MapGet("/api/ordering/cart/{customerId}", async (
    Guid customerId,
    ICartService cartService,
    CancellationToken cancellationToken) =>
{
    return Results.Ok(await cartService.GetAsync(customerId, cancellationToken));
});

app.MapPost("/api/ordering/cart/{customerId}/items", async (
    Guid customerId,
    AddCartItemRequest request,
    ICartService cartService,
    CancellationToken cancellationToken) =>
{
    return Results.Ok(await cartService.AddItemAsync(customerId, request.ProductId, request.Quantity, cancellationToken));
});

app.MapPut("/api/ordering/cart/{customerId}/items/{productId}", async (
    Guid customerId,
    Guid productId,
    UpdateCartItemQuantityRequest request,
    ICartService cartService,
    CancellationToken cancellationToken) =>
{
    return Results.Ok(await cartService.UpdateQuantityAsync(customerId, productId, request.Quantity, cancellationToken));
});

app.MapDelete("/api/ordering/cart/{customerId}/items/{productId}", async (
    Guid customerId,
    Guid productId,
    ICartService cartService,
    CancellationToken cancellationToken) =>
{
    return Results.Ok(await cartService.RemoveItemAsync(customerId, productId, cancellationToken));
});

app.MapPost("/api/ordering/checkout/{customerId}", async (
    Guid customerId,
    IOrderService orderService,
    CancellationToken cancellationToken) =>
{
    var order = await orderService.CheckoutAsync(customerId, cancellationToken);
    return Results.Created($"/api/ordering/orders/{order.Id}", order);
});

app.MapGet("/api/ordering/orders", async (
    ClaimsPrincipal principal,
    IOrderRepository orderRepository,
    CancellationToken cancellationToken) =>
{
    var orders = await orderRepository.GetByCustomerIdAsync(GetCustomerId(principal), cancellationToken);
    return Results.Ok(orders.Select(ToOrderDto));
})
.RequireAuthorization()
.WithName("GetOrderHistory");

app.MapGet("/api/ordering/orders/{orderId:guid}", async (
    Guid orderId,
    ClaimsPrincipal principal,
    IOrderRepository orderRepository,
    CancellationToken cancellationToken) =>
{
    var order = await orderRepository.GetByIdAsync(orderId, cancellationToken);

    // Toujours répondre 404 (jamais 403) quand la commande appartient à quelqu'un d'autre : on ne
    // révèle même pas qu'un identifiant deviné correspond à une commande qui existe.
    if (order is null || order.CustomerId != GetCustomerId(principal))
    {
        return Results.NotFound();
    }

    return Results.Ok(ToOrderDto(order));
})
.RequireAuthorization()
.WithName("GetOrder");

app.MapPost("/api/ordering/orders/{orderId}/payment-intent", async (
    Guid orderId,
    IPaymentService paymentService,
    CancellationToken cancellationToken) =>
{
    return Results.Ok(await paymentService.CreatePaymentIntentAsync(orderId, cancellationToken));
});

// Stripe envoie ce webhook pour confirmer (ou refuser) un paiement. On lit le corps brut de la
// requête (pas de liaison JSON automatique) car la vérification de signature a besoin du texte
// exact, octet pour octet, tel qu'envoyé par Stripe.
app.MapPost("/api/ordering/payments/webhook", async (
    HttpRequest request,
    IPaymentService paymentService,
    CancellationToken cancellationToken) =>
{
    using var reader = new StreamReader(request.Body);
    var payload = await reader.ReadToEndAsync(cancellationToken);
    var signature = request.Headers["Stripe-Signature"].ToString();

    await paymentService.HandleWebhookAsync(payload, signature, cancellationToken);
    return Results.Ok();
});

app.MapHealthChecks("/health/live");

app.Run();

static Guid GetCustomerId(ClaimsPrincipal principal) =>
    Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);

static OrderDto ToOrderDto(Order order)
{
    var items = order.Items
        .Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity, i.Subtotal))
        .ToList();

    return new OrderDto(order.Id, order.CustomerId, order.Status.ToString(), items, order.Total, order.CreatedAtUtc);
}

record AddCartItemRequest(Guid ProductId, int Quantity);
record UpdateCartItemQuantityRequest(int Quantity);

public partial class Program;
