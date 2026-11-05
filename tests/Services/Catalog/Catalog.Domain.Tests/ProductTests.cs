using Catalog.Domain;
using Catalog.Domain.Common;

namespace Catalog.Domain.Tests;

public class ProductTests
{
    [Fact]
    public void Create_WithValidData_GeneratesSlug()
    {
        var product = Product.Create("Clavier Mécanique RGB", 79.99m, 10, Guid.NewGuid());

        Assert.Equal("clavier-mecanique-rgb", product.Slug);
        Assert.Equal(10, product.Stock);
    }

    [Fact]
    public void Create_IsActiveByDefault()
    {
        var product = Product.Create("Produit", 10, 5, Guid.NewGuid());

        Assert.True(product.IsActive);
    }

    [Fact]
    public void Update_WithValidData_UpdatesFieldsAndRegeneratesSlug()
    {
        var categoryId = Guid.NewGuid();
        var newCategoryId = Guid.NewGuid();
        var product = Product.Create("Ancien Nom", 10, 5, categoryId);

        product.Update("Nouveau Nom", 20, 8, newCategoryId);

        Assert.Equal("Nouveau Nom", product.Name);
        Assert.Equal("nouveau-nom", product.Slug);
        Assert.Equal(20, product.Price);
        Assert.Equal(8, product.Stock);
        Assert.Equal(newCategoryId, product.CategoryId);
    }

    [Fact]
    public void Update_WithNegativePrice_ThrowsDomainException()
    {
        var product = Product.Create("Produit", 10, 5, Guid.NewGuid());

        Assert.Throws<DomainException>(() => product.Update("Produit", -1, 5, Guid.NewGuid()));
    }

    [Fact]
    public void Deactivate_SetsIsActiveToFalse()
    {
        var product = Product.Create("Produit", 10, 5, Guid.NewGuid());

        product.Deactivate();

        Assert.False(product.IsActive);
    }

    [Fact]
    public void Activate_AfterDeactivate_SetsIsActiveToTrue()
    {
        var product = Product.Create("Produit", 10, 5, Guid.NewGuid());
        product.Deactivate();

        product.Activate();

        Assert.True(product.IsActive);
    }

    [Fact]
    public void Create_WithNegativePrice_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Product.Create("Produit", -1, 10, Guid.NewGuid()));
    }

    [Fact]
    public void Create_WithNegativeStock_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Product.Create("Produit", 10, -1, Guid.NewGuid()));
    }

    [Fact]
    public void TryReserveStock_WithEnoughStock_ReturnsTrueAndDecreases()
    {
        var product = Product.Create("Produit", 10, 5, Guid.NewGuid());

        var reserved = product.TryReserveStock(3);

        Assert.True(reserved);
        Assert.Equal(2, product.Stock);
    }

    [Fact]
    public void TryReserveStock_WithInsufficientStock_ReturnsFalseAndLeavesStockUnchanged()
    {
        var product = Product.Create("Produit", 10, 2, Guid.NewGuid());

        var reserved = product.TryReserveStock(5);

        Assert.False(reserved);
        Assert.Equal(2, product.Stock);
    }

    [Fact]
    public void ReleaseStock_IncreasesStock()
    {
        var product = Product.Create("Produit", 10, 2, Guid.NewGuid());

        product.ReleaseStock(3);

        Assert.Equal(5, product.Stock);
    }
}
