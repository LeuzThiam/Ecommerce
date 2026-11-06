using Ordering.Domain;

namespace Ordering.Application;

public interface IInvoiceService
{
    byte[] GenerateInvoice(Order order);
}
