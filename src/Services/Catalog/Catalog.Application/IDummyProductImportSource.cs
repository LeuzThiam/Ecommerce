namespace Catalog.Application;

public sealed record ExternalProductDto(string Name, decimal Price, int Stock, string CategoryName);

public interface IDummyProductImportSource
{
    Task<IReadOnlyList<ExternalProductDto>> FetchAsync(int limit, CancellationToken cancellationToken);
}
