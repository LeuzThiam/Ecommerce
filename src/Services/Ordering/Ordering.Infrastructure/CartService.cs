using Ordering.Application;
using Ordering.Domain;
using Ordering.Domain.Common;

namespace Ordering.Infrastructure;

public sealed class CartService(ICartRepository repository, IProductCatalogClient catalogClient) : ICartService
{
    public async Task<CartDto> GetAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var cart = await GetOrCreateCartAsync(customerId, cancellationToken);
        return ToDto(cart);
    }

    public async Task<CartDto> AddItemAsync(Guid customerId, Guid productId, int quantity, CancellationToken cancellationToken)
    {
        var product = await catalogClient.GetProductAsync(productId, cancellationToken)
            ?? throw new DomainException("Ce produit n'existe pas.");

        if (product.Stock <= 0)
        {
            throw new DomainException("Ce produit n'est pas disponible.");
        }

        var cart = await GetOrCreateCartAsync(customerId, cancellationToken);
        cart.AddItem(product.Id, product.Name, product.Price, quantity);
        await repository.SaveChangesAsync(cancellationToken);

        return ToDto(cart);
    }

    public async Task<CartDto> UpdateQuantityAsync(Guid customerId, Guid productId, int quantity, CancellationToken cancellationToken)
    {
        var cart = await repository.GetByCustomerIdAsync(customerId, cancellationToken)
            ?? throw new DomainException("Le panier est vide.");

        cart.UpdateQuantity(productId, quantity);
        await repository.SaveChangesAsync(cancellationToken);

        return ToDto(cart);
    }

    public async Task<CartDto> RemoveItemAsync(Guid customerId, Guid productId, CancellationToken cancellationToken)
    {
        var cart = await repository.GetByCustomerIdAsync(customerId, cancellationToken)
            ?? throw new DomainException("Le panier est vide.");

        cart.RemoveItem(productId);
        await repository.SaveChangesAsync(cancellationToken);

        return ToDto(cart);
    }

    private async Task<Cart> GetOrCreateCartAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var cart = await repository.GetByCustomerIdAsync(customerId, cancellationToken);
        if (cart is not null)
        {
            return cart;
        }

        cart = Cart.Create(customerId);
        await repository.AddAsync(cart, cancellationToken);
        return cart;
    }

    private static CartDto ToDto(Cart cart)
    {
        var items = cart.Items
            .Select(i => new CartItemDto(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity, i.Subtotal))
            .ToList();

        return new CartDto(cart.Id, cart.CustomerId, items, cart.Total);
    }
}
