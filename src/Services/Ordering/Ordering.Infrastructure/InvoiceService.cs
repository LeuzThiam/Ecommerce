using Ordering.Application;
using Ordering.Domain;
using Ordering.Domain.Common;

namespace Ordering.Infrastructure;

public sealed class InvoiceService(IInvoicePdfGenerator pdfGenerator) : IInvoiceService
{
    public byte[] GenerateInvoice(Order order)
    {
        if (order.Status != OrderStatus.Paid)
        {
            throw new DomainException("Seule une commande payée dispose d'une facture.");
        }

        return pdfGenerator.Generate(order);
    }
}
