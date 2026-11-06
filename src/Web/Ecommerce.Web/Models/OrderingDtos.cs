namespace Ecommerce.Web.Models;

// Même principe que CatalogDtos.cs : copie locale de la forme des réponses d'Ordering.Api.
public sealed record CartItemDto(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal Subtotal);

public sealed record CartDto(Guid Id, Guid CustomerId, IReadOnlyList<CartItemDto> Items, decimal Total);

public sealed record OrderItemDto(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal Subtotal);

public sealed record PaymentAttemptDto(Guid Id, string Status, DateTime CreatedAtUtc);

public sealed record OrderDto(
    Guid Id,
    Guid CustomerId,
    string Status,
    IReadOnlyList<OrderItemDto> Items,
    decimal Total,
    DateTime CreatedAtUtc = default,
    IReadOnlyList<PaymentAttemptDto>? Payments = null);

// Résultat du téléchargement de facture : Bytes renseigné en cas de succès, ErrorMessage quand
// Ordering.Api a refusé (commande non payée) - tout le reste (ex. commande d'un autre client) se
// traduit par les deux à null, que le contrôleur traite comme un 404.
public sealed record InvoiceDownload(byte[]? Bytes, string? ErrorMessage);
