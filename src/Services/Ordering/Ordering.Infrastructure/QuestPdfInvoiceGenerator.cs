using Ordering.Application;
using Ordering.Domain;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Ordering.Infrastructure;

public sealed class QuestPdfInvoiceGenerator : IInvoicePdfGenerator
{
    public byte[] Generate(Order order)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(column =>
                {
                    column.Item().Text("Facture").FontSize(20).Bold();
                    column.Item().Text($"Commande {order.Id}");
                    column.Item().Text($"Émise le {order.CreatedAtUtc:dd/MM/yyyy}");
                });

                page.Content().PaddingVertical(15).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(4);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(2);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("Article").Bold();
                        header.Cell().Text("Prix unitaire").Bold();
                        header.Cell().Text("Quantité").Bold();
                        header.Cell().Text("Sous-total").Bold();
                    });

                    foreach (var item in order.Items)
                    {
                        table.Cell().Text(item.ProductName);
                        table.Cell().Text($"{item.UnitPrice:F2} €");
                        table.Cell().Text(item.Quantity.ToString());
                        table.Cell().Text($"{item.Subtotal:F2} €");
                    }
                });

                page.Footer().AlignRight().Text($"Total : {order.Total:F2} €").FontSize(14).Bold();
            });
        });

        return document.GeneratePdf();
    }
}
