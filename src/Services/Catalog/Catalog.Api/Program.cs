using System.Text;
using Catalog.Application;
using Catalog.Application.Common;
using Catalog.Domain.Common;
using Catalog.Infrastructure;
using Catalog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("La chaîne de connexion DefaultConnection est requise.");

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>();

builder.Services.AddScoped<IProductCatalog, EfProductCatalog>();
builder.Services.AddScoped<ICategoryAdminService, CategoryAdminService>();
builder.Services.AddScoped<IProductAdminService, ProductAdminService>();
builder.Services.AddScoped<IStockService, StockService>();

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

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy => policy.RequireRole("Administrator"));

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

app.MapGet("/api/catalog/products", async (
    IProductCatalog catalog,
    string? query,
    Guid? categoryId,
    decimal? minimumPrice,
    decimal? maximumPrice,
    string sort,
    int page,
    int pageSize,
    CancellationToken cancellationToken) =>
{
    var search = new ProductSearch(query, categoryId, minimumPrice, maximumPrice,
        string.IsNullOrWhiteSpace(sort) ? "name" : sort,
        page <= 0 ? 1 : page,
        pageSize <= 0 ? 20 : pageSize);

    var result = await catalog.SearchAsync(search, cancellationToken);
    return Results.Ok(result);
});

app.MapGet("/api/catalog/products/{slug}", async (
    string slug,
    IProductCatalog catalog,
    CancellationToken cancellationToken) =>
{
    var product = await catalog.GetBySlugAsync(slug, cancellationToken);
    return product is null ? Results.NotFound() : Results.Ok(product);
});

// Utilisé par les autres services (ex: Ordering.Api) pour récupérer le prix/stock actuel
// d'un produit par son identifiant, plutôt que par son slug (destiné aux clients web).
app.MapGet("/api/catalog/products/by-id/{id}", async (
    Guid id,
    IProductCatalog catalog,
    CancellationToken cancellationToken) =>
{
    var product = await catalog.GetByIdAsync(id, cancellationToken);
    return product is null ? Results.NotFound() : Results.Ok(product);
});

app.MapGet("/api/catalog/categories", async (
    ICategoryAdminService categories,
    CancellationToken cancellationToken) =>
{
    return Results.Ok(await categories.ListAsync(cancellationToken));
});

app.MapPost("/api/catalog/categories", async (
    CreateCategoryRequest request,
    ICategoryAdminService categories,
    CancellationToken cancellationToken) =>
{
    var category = await categories.CreateAsync(request.Name, cancellationToken);
    return Results.Created($"/api/catalog/categories/{category.Id}", category);
})
.RequireAuthorization("AdminOnly");

app.MapPost("/api/catalog/products", async (
    CreateProductRequest request,
    IProductAdminService products,
    CancellationToken cancellationToken) =>
{
    var product = await products.CreateAsync(request.Name, request.Price, request.Stock, request.CategoryId, cancellationToken);
    return Results.Created($"/api/catalog/products/{product.Slug}", product);
})
.RequireAuthorization("AdminOnly");

// Endpoints internes, appelés par Ordering.Api au moment du checkout — pas destinés aux clients.
app.MapPost("/api/catalog/stock/reserve", async (
    ReserveStockRequest request,
    IStockService stock,
    CancellationToken cancellationToken) =>
{
    var reserved = await stock.TryReserveAsync(request.ProductId, request.Quantity, cancellationToken);
    return reserved ? Results.Ok() : Results.Conflict(new { message = "Stock insuffisant ou indisponible." });
});

app.MapPost("/api/catalog/stock/release", async (
    ReserveStockRequest request,
    IStockService stock,
    CancellationToken cancellationToken) =>
{
    await stock.ReleaseAsync(request.ProductId, request.Quantity, cancellationToken);
    return Results.Ok();
});

app.MapHealthChecks("/health/live");

app.Run();

record CreateCategoryRequest(string Name);
record CreateProductRequest(string Name, decimal Price, int Stock, Guid CategoryId);
record ReserveStockRequest(Guid ProductId, int Quantity);

public partial class Program;
