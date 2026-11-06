using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ecommerce.Web.Models;

namespace Ecommerce.Web.Clients;

public sealed class IdentityApiClient(HttpClient httpClient) : IIdentityApiClient
{
    public async Task<LoginResult?> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync("/api/identity/login", new { email, password }, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken);
        return body is null ? null : new LoginResult(body.AccessToken);
    }

    public async Task<UserDto?> GetProfileAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/identity/profile", accessToken);
        var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken);
    }

    public async Task<UserDto?> UpdateProfileAsync(string accessToken, string displayName, CancellationToken cancellationToken)
    {
        using var request = CreateAuthenticatedRequest(HttpMethod.Put, "/api/identity/profile", accessToken);
        request.Content = JsonContent.Create(new { displayName });
        var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken);
    }

    public async Task<IReadOnlyList<AddressDto>> GetAddressesAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/identity/addresses", accessToken);
        var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        var addresses = await response.Content.ReadFromJsonAsync<List<AddressDto>>(cancellationToken);
        return addresses ?? [];
    }

    public async Task AddAddressAsync(
        string accessToken,
        string line1,
        string? line2,
        string city,
        string postalCode,
        string country,
        CancellationToken cancellationToken)
    {
        using var request = CreateAuthenticatedRequest(HttpMethod.Post, "/api/identity/addresses", accessToken);
        request.Content = JsonContent.Create(new { line1, line2, city, postalCode, country });
        var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    // HttpClient.DefaultRequestHeaders est mutable et partagé entre requêtes concurrentes via le
    // pool du HttpClientFactory - on pose donc l'en-tête Authorization par requête plutôt que dessus.
    private static HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string uri, string accessToken)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    private sealed record LoginResponse(string AccessToken);
}
