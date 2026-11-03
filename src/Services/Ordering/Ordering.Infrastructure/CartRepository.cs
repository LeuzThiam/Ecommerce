using Microsoft.EntityFrameworkCore;
using Ordering.Application;
using Ordering.Domain;
using Ordering.Infrastructure.Persistence;

namespace Ordering.Infrastructure;

public sealed class CartRepository(ApplicationDbContext dbContext) : ICartRepository
{
    public async Task<Cart?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken)
    {
        // Sans tri explicite, EF Core ne garantit aucun ordre de retour pour une collection —
        // on trie par date d'ajout pour que l'ordre des articles soit stable et prévisible
        // (pour l'affichage comme pour le traitement séquentiel au checkout).
        return await dbContext.Carts
            .Include(c => c.Items.OrderBy(i => i.AddedAtUtc))
            .FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);
    }

    public async Task AddAsync(Cart cart, CancellationToken cancellationToken)
    {
        await dbContext.Carts.AddAsync(cart, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
