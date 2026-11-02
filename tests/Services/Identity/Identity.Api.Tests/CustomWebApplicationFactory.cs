using Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Identity.Api.Tests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        // Program.cs lit ConnectionStrings:DefaultConnection dès le démarrage, avant même que
        // nos remplacements de services ci-dessous ne s'appliquent. La vraie valeur n'a aucune
        // importance ici puisqu'on remplace complètement le DbContext juste en dessous — il
        // suffit qu'elle ne soit pas vide pour passer le "?? throw" de Program.cs.
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Data Source=:memory:");

        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // UseSqlServer (dans le vrai Program.cs) enregistre les services internes du
            // fournisseur SQL Server dans le conteneur. RemoveAll<DbContextOptions<T>> seul ne
            // les enlève pas, et EF Core refuse de démarrer si deux fournisseurs de base de
            // données sont enregistrés en même temps — on retire donc tout ce qui vient d'EF Core
            // avant de reconstruire la configuration avec SQLite.
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