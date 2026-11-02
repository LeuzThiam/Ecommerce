using Catalog.Application;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure;

public sealed class StockService(ApplicationDbContext dbContext) : IStockService
{
    public async Task<bool> TryReserveAsync(Guid productId, int quantity, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.FindAsync([productId], cancellationToken);
        if (product is null || !product.TryReserveStock(quantity))
        {
            return false;
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // Un autre processus a modifié le stock entre notre lecture et notre écriture
            // (RowVersion différent au moment du SaveChanges). On ne retente pas
            // automatiquement ici : on signale simplement l'échec, l'appelant décide quoi faire.
            return false;
        }
    }

    public async Task ReleaseAsync(Guid productId, int quantity, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.FindAsync([productId], cancellationToken);
        if (product is null)
        {
            return;
        }

        product.ReleaseStock(quantity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
