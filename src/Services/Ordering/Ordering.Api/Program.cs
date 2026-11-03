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

builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<ICartService, CartService>();

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

app.MapGet("/health/live", () => Results.Ok());

app.Run();

record AddCartItemRequest(Guid ProductId, int Quantity);
record UpdateCartItemQuantityRequest(int Quantity);

public partial class Program;
