using Ecommerce.Web.Models;

namespace Ecommerce.Web.Clients;

public interface IOrderingApiClient
{
    Task<CartDto> GetCartAsync(Guid customerId, CancellationToken cancellationToken);

    Task<CartDto?> AddItemAsync(Guid customerId, Guid productId, int quantity, CancellationToken cancellationToken);

    Task<CartDto?> UpdateItemQuantityAsync(Guid customerId, Guid productId, int quantity, CancellationToken cancellationToken);

    Task<CartDto?> RemoveItemAsync(Guid customerId, Guid productId, CancellationToken cancellationToken);

    Task<OrderDto?> CheckoutAsync(Guid customerId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OrderDto>> GetOrderHistoryAsync(string accessToken, CancellationToken cancellationToken);

    Task<OrderDto?> GetOrderAsync(string accessToken, Guid orderId, CancellationToken cancellationToken);

    Task<InvoiceDownload> GetInvoiceAsync(string accessToken, Guid orderId, CancellationToken cancellationToken);

    // Vue administrateur : toutes les commandes de tous les clients.
    Task<IReadOnlyList<OrderDto>> GetAllOrdersAdminAsync(string accessToken, CancellationToken cancellationToken);

    Task<OrderDto?> GetOrderAdminAsync(string accessToken, Guid orderId, CancellationToken cancellationToken);

    Task RefundOrderAsync(string accessToken, Guid orderId, CancellationToken cancellationToken);
}
