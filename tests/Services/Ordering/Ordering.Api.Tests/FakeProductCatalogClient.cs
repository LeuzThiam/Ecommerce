using Ordering.Application;

namespace Ordering.Api.Tests;

// Remplace HttpProductCatalogClient dans les tests — évite de dépendre d'un vrai Catalog.Api
// en marche. Chaque test configure lui-même les produits "connus" avant d'appeler le panier.
public sealed class FakeProductCatalogClient : IProductCatalogClient
{
    private readonly Dictionary<Guid, ProductSnapshot> _products = [];

    public void AddProduct(ProductSnapshot product) => _products[product.Id] = product;

    public Task<ProductSnapshot?> GetProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_products.GetValueOrDefault(productId));
    }
}
