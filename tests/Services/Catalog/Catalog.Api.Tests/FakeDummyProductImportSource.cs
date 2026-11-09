using Catalog.Application;

namespace Catalog.Api.Tests;

// Remplace DummyJsonProductImportSource dans les tests — évite tout appel réseau réel vers
// dummyjson.com pendant la suite de tests.
public sealed class FakeDummyProductImportSource : IDummyProductImportSource
{
    private IReadOnlyList<ExternalProductDto> _products = [];

    public void SetProducts(params ExternalProductDto[] products) => _products = products;

    public Task<IReadOnlyList<ExternalProductDto>> FetchAsync(int limit, CancellationToken cancellationToken) =>
        Task.FromResult(_products);
}
