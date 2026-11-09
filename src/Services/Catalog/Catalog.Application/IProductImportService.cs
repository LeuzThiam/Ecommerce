namespace Catalog.Application;

public sealed record ProductImportResultDto(int Imported, int Skipped);

public interface IProductImportService
{
    Task<ProductImportResultDto> ImportDummyProductsAsync(int limit, CancellationToken cancellationToken);
}
