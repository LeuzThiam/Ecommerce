using Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override int SaveChanges()
    {
        IncrementConcurrencyVersions();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        IncrementConcurrencyVersions();
        return base.SaveChangesAsync(cancellationToken);
    }

    // On gère nous-mêmes l'incrémentation du jeton de concurrence "Version" (voir
    // ProductConfiguration) plutôt que de dépendre d'une fonctionnalité auto-générée par la base
    // de données (qui diffère selon le fournisseur). À chaque sauvegarde d'un Product modifié,
    // on fixe l'OriginalValue à la valeur qu'on a lue au départ (ce qu'EF Core utilisera dans la
    // clause WHERE du UPDATE), puis on augmente la valeur à écrire de 1. Si un autre processus a
    // déjà modifié cette ligne entre-temps, la clause WHERE ne correspondra à aucune ligne, et EF
    // Core lèvera DbUpdateConcurrencyException.
    private void IncrementConcurrencyVersions()
    {
        foreach (var entry in ChangeTracker.Entries<Product>())
        {
            if (entry.State != EntityState.Modified)
            {
                continue;
            }

            var versionProperty = entry.Property<int>("Version");
            var currentValue = versionProperty.CurrentValue;
            versionProperty.OriginalValue = currentValue;
            versionProperty.CurrentValue = currentValue + 1;
        }
    }
}
