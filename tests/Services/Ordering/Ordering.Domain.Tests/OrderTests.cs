using Ordering.Domain;
using Ordering.Domain.Common;

namespace Ordering.Domain.Tests;

public class OrderTests
{
    [Fact]
    public void Create_WithItems_CreatesPendingPaymentOrder()
    {
        var customerId = Guid.NewGuid();
        var item = OrderItem.Create(Guid.NewGuid(), "Produit", 10m, 2);

        var order = Order.Create(customerId, [item]);

        Assert.Equal(OrderStatus.PendingPayment, order.Status);
        Assert.Equal(20m, order.Total);
        Assert.Single(order.Items);
    }

    [Fact]
    public void Create_WithNoItems_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Order.Create(Guid.NewGuid(), []));
    }

    [Fact]
    public void Create_WithEmptyCustomerId_ThrowsDomainException()
    {
        var item = OrderItem.Create(Guid.NewGuid(), "Produit", 10m, 1);

        Assert.Throws<DomainException>(() => Order.Create(Guid.Empty, [item]));
    }
}
