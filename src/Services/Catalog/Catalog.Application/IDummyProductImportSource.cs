namespace Catalog.Application;

public sealed record ExternalProductDto(string Name, decimal Price, int Stock, string CategoryName, string? ImageUrl = null);

public interface IDummyProductImportSource
{
    Task<IReadOnlyList<ExternalProductDto>> FetchAsync(int limit, CancellationToken cancellationToken);
}
