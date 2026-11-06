using System.Globalization;
using Ecommerce.Web.Clients;
using Ecommerce.Web.Filters;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;

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
// Un <input type="number"> HTML envoie toujours ses valeurs avec un point décimal, quelle que
// soit la langue du navigateur (spec HTML5) - mais le binding de modèle ASP.NET Core utilise par
// défaut la culture du système, qui peut attendre une virgule (ex: fr-FR). Sans ça, un formulaire
// admin posté depuis une machine en culture française tronquerait silencieusement "19.99" à 0.
var invariantCulture = new CultureInfo("en-US");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(invariantCulture),
    SupportedCultures = [invariantCulture],
    SupportedUICultures = [invariantCulture],
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Products}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
