using Catalog.Application;

namespace Catalog.Infrastructure;

public sealed class ProductImportService(
    IDummyProductImportSource importSource,
    IProductCatalog productCatalog,
    ICategoryAdminService categoryAdminService,
    IProductAdminService productAdminService) : IProductImportService
{
    public async Task<ProductImportResultDto> ImportDummyProductsAsync(int limit, CancellationToken cancellationToken)
    {
        var externalProducts = await importSource.FetchAsync(limit, cancellationToken);

        // Dedoublonnage par nom (meme garde-fou que l'ancien DummyJsonService) - une recherche
        // incluant les produits desactives pour ne jamais reimporter un produit juste desactive.
        var searchResult = await productCatalog.SearchAsync(
            new ProductSearch(Query: null, CategoryId: null, MinimumPrice: null, MaximumPrice: null,
                Sort: "name", Page: 1, PageSize: 1000, IncludeInactive: true),
            cancellationToken);
        var existingNames = searchResult.Items
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // dummyjson.com ne donne qu'un nom de categorie en texte libre ("smartphones", "beauty"...)
        // - contrairement a l'ancien DummyJsonService qui stockait ça tel quel dans une colonne
        // CategorieNom, on cree ou reutilise ici une vraie Category avec une vraie relation.
        // GroupBy plutôt que ToDictionary : rien dans le Domain n'empêche deux catégories de
        // partager le même nom (pas d'index unique), donc un ToDictionary planterait dès qu'un
        // doublon existe déjà en base - on prend simplement la première trouvée dans ce cas.
        var categories = (await categoryAdminService.ListAsync(cancellationToken))
            .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

        var imported = 0;
        var skipped = 0;

        foreach (var external in externalProducts)
        {
            if (existingNames.Contains(external.Name))
            {
                skipped++;
                continue;
            }

            if (!categories.TryGetValue(external.CategoryName, out var categoryId))
            {
                var category = await categoryAdminService.CreateAsync(external.CategoryName, cancellationToken);
                categoryId = category.Id;
                categories[external.CategoryName] = categoryId;
            }

            await productAdminService.CreateAsync(external.Name, external.Price, external.Stock, categoryId, cancellationToken);
            existingNames.Add(external.Name);
            imported++;
        }

        return new ProductImportResultDto(imported, skipped);
    }
}
