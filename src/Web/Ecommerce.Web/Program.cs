using Ecommerce.Web.Clients;
using Ecommerce.Web.Filters;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options => options.Filters.Add<ApiExceptionFilter>());

var gatewayBaseUrl = builder.Configuration["Gateway:BaseUrl"]
    ?? throw new InvalidOperationException("La configuration Gateway:BaseUrl est requise.");

builder.Services.AddHttpClient<ICatalogApiClient, CatalogApiClient>(client =>
{
    client.BaseAddress = new Uri(gatewayBaseUrl);
});

builder.Services.AddHttpClient<IOrderingApiClient, OrderingApiClient>(client =>
{
    client.BaseAddress = new Uri(gatewayBaseUrl);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
