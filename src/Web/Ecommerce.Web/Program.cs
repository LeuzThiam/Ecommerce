using Ecommerce.Web.Clients;
using Ecommerce.Web.Filters;
using Microsoft.AspNetCore.Authentication.Cookies;

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

builder.Services.AddHttpClient<IIdentityApiClient, IdentityApiClient>(client =>
{
    client.BaseAddress = new Uri(gatewayBaseUrl);
});

// Le Web n'a pas sa propre notion d'utilisateur : le cookie ne fait que porter le JWT émis par
// Identity.Api (claim "access_token"), reposé tel quel en en-tête Authorization vers les API en
// amont à chaque requête authentifiée.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
