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

    private readonly SqliteConnection _connection = new("Data Source=:memory:");

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
        _connection.Open();

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

            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));

            using var scope = services.BuildServiceProvider().CreateScope();
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection.Dispose();
    }
}
