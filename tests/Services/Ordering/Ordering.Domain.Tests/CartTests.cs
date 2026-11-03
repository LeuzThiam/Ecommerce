using Ordering.Domain;
using Ordering.Domain.Common;

namespace Ordering.Domain.Tests;

public class CartTests
{
    [Fact]
    public void AddItem_NewProduct_AddsItem()
    {
        var cart = Cart.Create(Guid.NewGuid());
        var productId = Guid.NewGuid();

        cart.AddItem(productId, "Produit", 10m, 2);

        var item = Assert.Single(cart.Items);
        Assert.Equal(productId, item.ProductId);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(20m, cart.Total);
    }

    [Fact]
    public void AddItem_SameProductTwice_MergesQuantity()
    {
        var cart = Cart.Create(Guid.NewGuid());
        var productId = Guid.NewGuid();

        cart.AddItem(productId, "Produit", 10m, 2);
        cart.AddItem(productId, "Produit", 10m, 3);

        var item = Assert.Single(cart.Items);
        Assert.Equal(5, item.Quantity);
    }

    [Fact]
    public void UpdateQuantity_ToZero_RemovesItem()
    {
        var cart = Cart.Create(Guid.NewGuid());
        var productId = Guid.NewGuid();
        cart.AddItem(productId, "Produit", 10m, 2);

        cart.UpdateQuantity(productId, 0);

        Assert.Empty(cart.Items);
    }

    [Fact]
    public void UpdateQuantity_ProductNotInCart_ThrowsDomainException()
    {
        var cart = Cart.Create(Guid.NewGuid());

        Assert.Throws<DomainException>(() => cart.UpdateQuantity(Guid.NewGuid(), 1));
    }

    [Fact]
    public void RemoveItem_ExistingProduct_RemovesIt()
    {
        var cart = Cart.Create(Guid.NewGuid());
        var productId = Guid.NewGuid();
        cart.AddItem(productId, "Produit", 10m, 2);

        cart.RemoveItem(productId);

        Assert.Empty(cart.Items);
    }
}
