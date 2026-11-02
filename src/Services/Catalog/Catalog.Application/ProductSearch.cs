namespace Catalog.Application;

public sealed record ProductSearch(
    string? Query,
    Guid? CategoryId,
    decimal? MinimumPrice,
    decimal? MaximumPrice,
    string Sort = "name",
    int Page = 1,
    int PageSize = 20);
