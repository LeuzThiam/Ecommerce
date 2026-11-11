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
        // Dictionnaire (pas juste un HashSet) : un ré-import doit pouvoir retrouver l'Id d'un
        // produit déjà présent pour y rattacher les attributs riches qu'un import plus ancien
        // (avant l'ajout d'ImageUrl/ProductExternalDetails) n'avait pas pu capturer.
        var searchResult = await productCatalog.SearchAsync(
            new ProductSearch(Query: null, CategoryId: null, MinimumPrice: null, MaximumPrice: null,
                Sort: "name", Page: 1, PageSize: 1000, IncludeInactive: true),
            cancellationToken);
        var existingProducts = searchResult.Items
            .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

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
            if (existingProducts.TryGetValue(external.Name, out var existingId))
            {
                // Déjà importé - on rafraîchit quand même ses attributs riches, au cas où il
                // viendrait d'un import plus ancien qui ne les capturait pas encore (c'est
                // exactement le cas qui a motivé ce ré-import : des produits dummyjson sans image).
                await productAdminService.SetExternalDetailsAsync(existingId, external, cancellationToken);
                skipped++;
                continue;
            }

            if (!categories.TryGetValue(external.CategoryName, out var categoryId))
            {
                var category = await categoryAdminService.CreateAsync(external.CategoryName, cancellationToken);
                categoryId = category.Id;
                categories[external.CategoryName] = categoryId;
            }

            var created = await productAdminService.CreateAsync(external.Name, external.Price, external.Stock, categoryId, external.ImageUrl, cancellationToken);
            await productAdminService.SetExternalDetailsAsync(created.Id, external, cancellationToken);
            existingProducts[external.Name] = created.Id;
            imported++;
        }

        return new ProductImportResultDto(imported, skipped);
    }
}
