using System.Net.Http.Json;
using Ordering.Application;

namespace Ordering.Infrastructure;

public sealed class HttpStockReservationClient(HttpClient httpClient) : IStockReservationClient
{
    public async Task<bool> TryReserveAsync(Guid productId, int quantity, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync("/api/catalog/stock/reserve", new { productId, quantity }, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task ReleaseAsync(Guid productId, int quantity, CancellationToken cancellationToken)
    {
        await httpClient.PostAsJsonAsync("/api/catalog/stock/release", new { productId, quantity }, cancellationToken);
    }
}
