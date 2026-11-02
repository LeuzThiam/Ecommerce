using Catalog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Api.Tests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Data Source=:memory:");

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
