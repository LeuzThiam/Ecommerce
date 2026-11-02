using Catalog.Application;
using Catalog.Application.Common;
using Catalog.Domain.Common;
using Catalog.Infrastructure;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("La chaîne de connexion DefaultConnection est requise.");

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddScoped<IProductCatalog, EfProductCatalog>();
builder.Services.AddScoped<ICategoryAdminService, CategoryAdminService>();
builder.Services.AddScoped<IProductAdminService, ProductAdminService>();
builder.Services.AddScoped<IStockService, StockService>();

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

app.MapGet("/api/catalog/categories", async (
    ICategoryAdminService categories,
    CancellationToken cancellationToken) =>
{
    return Results.Ok(await categories.ListAsync(cancellationToken));
});

// TODO (Issue #10) : protéger avec [Authorize(Roles = "Administrator")] une fois le schéma
// JWT inter-services résolu — voir la note de l'Issue #3.
app.MapPost("/api/catalog/categories", async (
    CreateCategoryRequest request,
    ICategoryAdminService categories,
    CancellationToken cancellationToken) =>
{
    try
    {
        var category = await categories.CreateAsync(request.Name, cancellationToken);
        return Results.Created($"/api/catalog/categories/{category.Id}", category);
    }
    catch (DomainException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

// TODO (Issue #10) : protéger avec [Authorize(Roles = "Administrator")].
app.MapPost("/api/catalog/products", async (
    CreateProductRequest request,
    IProductAdminService products,
    CancellationToken cancellationToken) =>
{
    try
    {
        var product = await products.CreateAsync(request.Name, request.Price, request.Stock, request.CategoryId, cancellationToken);
        return Results.Created($"/api/catalog/products/{product.Slug}", product);
    }
    catch (DomainException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

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

app.MapGet("/health/live", () => Results.Ok());

app.Run();

record CreateCategoryRequest(string Name);
record CreateProductRequest(string Name, decimal Price, int Stock, Guid CategoryId);
record ReserveStockRequest(Guid ProductId, int Quantity);

public partial class Program;
