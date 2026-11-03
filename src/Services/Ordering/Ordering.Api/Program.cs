using Microsoft.EntityFrameworkCore;
using Ordering.Application;
using Ordering.Domain.Common;
using Ordering.Infrastructure;
using Ordering.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("La chaîne de connexion DefaultConnection est requise.");

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

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

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// TODO (Issue #10) : remplacer customerId en paramètre de route par l'identité extraite du JWT
// une fois le schéma d'autorisation inter-services résolu — voir la note de l'Issue #3.

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
    try
    {
        return Results.Ok(await cartService.AddItemAsync(customerId, request.ProductId, request.Quantity, cancellationToken));
    }
    catch (DomainException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

app.MapPut("/api/ordering/cart/{customerId}/items/{productId}", async (
    Guid customerId,
    Guid productId,
    UpdateCartItemQuantityRequest request,
    ICartService cartService,
    CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await cartService.UpdateQuantityAsync(customerId, productId, request.Quantity, cancellationToken));
    }
    catch (DomainException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

app.MapDelete("/api/ordering/cart/{customerId}/items/{productId}", async (
    Guid customerId,
    Guid productId,
    ICartService cartService,
    CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await cartService.RemoveItemAsync(customerId, productId, cancellationToken));
    }
    catch (DomainException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

app.MapPost("/api/ordering/checkout/{customerId}", async (
    Guid customerId,
    IOrderService orderService,
    CancellationToken cancellationToken) =>
{
    try
    {
        var order = await orderService.CheckoutAsync(customerId, cancellationToken);
        return Results.Created($"/api/ordering/orders/{order.Id}", order);
    }
    catch (DomainException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

app.MapPost("/api/ordering/orders/{orderId}/payment-intent", async (
    Guid orderId,
    IPaymentService paymentService,
    CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await paymentService.CreatePaymentIntentAsync(orderId, cancellationToken));
    }
    catch (DomainException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
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

app.MapGet("/health/live", () => Results.Ok());

app.Run();

record AddCartItemRequest(Guid ProductId, int Quantity);
record UpdateCartItemQuantityRequest(int Quantity);

public partial class Program;
