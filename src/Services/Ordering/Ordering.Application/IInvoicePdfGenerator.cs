using Ordering.Domain;

namespace Ordering.Application;

// Abstraction au-dessus de la bibliothèque de génération PDF - l'Application ne connaît que ce
// contrat, jamais QuestPDF directement.
public interface IInvoicePdfGenerator
{
    byte[] Generate(Order order);
}
