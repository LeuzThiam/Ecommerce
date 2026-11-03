using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ordering.Application;
using Ordering.Infrastructure.Persistence;

namespace Ordering.Api.Tests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    public FakeProductCatalogClient CatalogClient { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Data Source=:memory:");
        Environment.SetEnvironmentVariable("Services__CatalogApi", "http://localhost");

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

            // Remplace le vrai client HTTP par notre faux, configurable depuis les tests.
            services.RemoveAll<IProductCatalogClient>();
            services.AddSingleton<IProductCatalogClient>(CatalogClient);

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
