using Catalog.Domain;
using Catalog.Domain.Common;

namespace Catalog.Domain.Tests;

public class ProductVariantTests
{
    [Fact]
    public void AddVariant_WithValidData_AddsToVariantsList()
    {
        var product = Product.Create("T-Shirt", 20m, 0, Guid.NewGuid());

        var variant = product.AddVariant("Rouge - M", 22m, 5);

        Assert.Single(product.Variants);
        Assert.Equal(variant.Id, product.Variants[0].Id);
    }

    [Fact]
    public void AddVariant_TwoVariants_HaveIndependentPriceAndStock()
    {
        var product = Product.Create("T-Shirt", 20m, 0, Guid.NewGuid());

        var small = product.AddVariant("Rouge - S", 19.99m, 0);
        var medium = product.AddVariant("Rouge - M", 22.50m, 7);

        Assert.Equal(0, small.Stock);
        Assert.Equal(7, medium.Stock);
        Assert.NotEqual(small.Price, medium.Price);
    }

    [Fact]
    public void AddVariant_WithEmptyName_ThrowsDomainException()
    {
        var product = Product.Create("T-Shirt", 20m, 0, Guid.NewGuid());

        Assert.Throws<DomainException>(() => product.AddVariant("", 22m, 5));
    }

    [Fact]
    public void AddVariant_WithNegativePrice_ThrowsDomainException()
    {
        var product = Product.Create("T-Shirt", 20m, 0, Guid.NewGuid());

        Assert.Throws<DomainException>(() => product.AddVariant("Rouge - M", -1m, 5));
    }

    [Fact]
    public void AddVariant_WithNegativeStock_ThrowsDomainException()
    {
        var product = Product.Create("T-Shirt", 20m, 0, Guid.NewGuid());

        Assert.Throws<DomainException>(() => product.AddVariant("Rouge - M", 22m, -1));
    }
}
