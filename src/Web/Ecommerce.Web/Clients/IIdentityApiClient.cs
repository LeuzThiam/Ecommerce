using Ecommerce.Web.Models;

namespace Ecommerce.Web.Clients;

public sealed record LoginResult(string AccessToken);

public sealed record RegisterResult(bool Success, string? ErrorMessage = null);

public interface IIdentityApiClient
{
    Task<LoginResult?> LoginAsync(string email, string password, CancellationToken cancellationToken);

    Task<RegisterResult> RegisterAsync(string email, string password, string displayName, CancellationToken cancellationToken);

    Task<UserDto?> GetProfileAsync(string accessToken, CancellationToken cancellationToken);

    Task<UserDto?> UpdateProfileAsync(string accessToken, string displayName, CancellationToken cancellationToken);

    Task<IReadOnlyList<AddressDto>> GetAddressesAsync(string accessToken, CancellationToken cancellationToken);

    Task AddAddressAsync(
        string accessToken,
        string line1,
        string? line2,
        string city,
        string postalCode,
        string country,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<UserAdminDto>> GetAllUsersAdminAsync(string accessToken, CancellationToken cancellationToken);

    Task PromoteUserAsync(string accessToken, Guid userId, CancellationToken cancellationToken);

    Task DemoteUserAsync(string accessToken, Guid userId, CancellationToken cancellationToken);
}
