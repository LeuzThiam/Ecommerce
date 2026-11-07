using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Catalog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Catalog.Api.Tests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Clé de test uniquement — jamais utilisée en dehors de ce projet, aucun lien avec une
    // vraie clé de production. Les mêmes valeurs côté Program.cs (via la variable
    // d'environnement ci-dessous) permettent de générer ici des tokens que l'application
    // testée acceptera comme réellement valides.
    private const string TestJwtSecretKey = "test-secret-key-never-used-outside-of-tests-0123456789";

    // "cache=shared" + un nom unique par instance de factory (pas juste ":memory:") : chaque
    // DbContext ouvre sa PROPRE connexion vers la même base en mémoire, au lieu de partager un
    // seul objet SqliteConnection entre toutes les requêtes. Avec une connexion unique partagée,
    // deux requêtes HTTP vraiment concurrentes (StockReservationTests) exécutent des commandes
    // sur le même objet SqliteConnection depuis deux threads différents — non thread-safe côté
    // Microsoft.Data.Sqlite, ce qui provoquait un résultat incohérent (aucune des deux requêtes
    // ne réussit) de façon instable, observé uniquement sur les runners CI (jamais en local).
    // On garde une connexion ouverte pour la durée de vie de la factory uniquement pour empêcher
    // SQLite de supprimer la base en mémoire dès que la dernière connexion se ferme.
    private readonly string _connectionString = $"Data Source=file:catalog-tests-{Guid.NewGuid():N}?mode=memory&cache=shared";
    private SqliteConnection? _keepAliveConnection;

    public string GenerateToken(params string[] roles)
    {
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()) };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "Ecommerce",
            audience: "Ecommerce",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _keepAliveConnection = new SqliteConnection(_connectionString);
        _keepAliveConnection.Open();

        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Data Source=:memory:");
        Environment.SetEnvironmentVariable("Jwt__SecretKey", TestJwtSecretKey);

        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var efCoreDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true
                    || d.ServiceType == typeof(ApplicationDbContext))
                .ToList();

            foreach (var descriptor in efCoreDescriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connectionString));

            using var scope = services.BuildServiceProvider().CreateScope();
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _keepAliveConnection?.Dispose();
    }
}
