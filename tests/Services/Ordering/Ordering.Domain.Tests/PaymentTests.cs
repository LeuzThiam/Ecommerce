using Ordering.Domain;
using Ordering.Domain.Common;

namespace Ordering.Domain.Tests;

public class PaymentTests
{
    [Fact]
    public void Create_WithPositiveAmount_CreatesPendingPayment()
    {
        var payment = Payment.Create(Guid.NewGuid(), 50m);

        Assert.Equal(PaymentStatus.Pending, payment.Status);
    }

    [Fact]
    public void Create_WithZeroOrNegativeAmount_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Payment.Create(Guid.NewGuid(), 0));
    }

    [Fact]
    public void MarkAsSucceeded_WhenPending_ChangesStatus()
    {
        var payment = Payment.Create(Guid.NewGuid(), 50m);

        payment.MarkAsSucceeded();

        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
    }

    [Fact]
    public void MarkAsSucceeded_WhenAlreadySucceeded_ThrowsDomainException()
    {
        var payment = Payment.Create(Guid.NewGuid(), 50m);
        payment.MarkAsSucceeded();

        Assert.Throws<DomainException>(() => payment.MarkAsSucceeded());
    }
}
