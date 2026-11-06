using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Identity.Api.Tests;

public class ProfileAndAddressTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task AddAddress_FirstAddress_AppearsInListAndIsDefault()
    {
        await AuthenticateAsync("address1@example.com");

        var response = await _client.PostAsJsonAsync("/api/identity/addresses", new
        {
            line1 = "12 rue de la Paix",
            line2 = (string?)null,
            city = "Paris",
            postalCode = "75002",
            country = "France"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var addresses = await (await _client.GetAsync("/api/identity/addresses")).Content.ReadFromJsonAsync<List<AddressResponse>>();

        Assert.Single(addresses!);
        Assert.True(addresses![0].IsDefault);
    }

    [Fact]
    public async Task SetDefaultAddress_MarksSelectedAddressAsDefault()
    {
        await AuthenticateAsync("address2@example.com");

        await _client.PostAsJsonAsync("/api/identity/addresses", new { line1 = "A", line2 = (string?)null, city = "Paris", postalCode = "75002", country = "France" });
        var secondResponse = await _client.PostAsJsonAsync("/api/identity/addresses", new { line1 = "B", line2 = (string?)null, city = "Lyon", postalCode = "69002", country = "France" });
        var second = await secondResponse.Content.ReadFromJsonAsync<AddressResponse>();

        var setDefaultResponse = await _client.PostAsync($"/api/identity/addresses/{second!.Id}/default", null);
        Assert.Equal(HttpStatusCode.NoContent, setDefaultResponse.StatusCode);

        var addresses = await (await _client.GetAsync("/api/identity/addresses")).Content.ReadFromJsonAsync<List<AddressResponse>>();

        Assert.True(addresses!.Single(a => a.Id == second.Id).IsDefault);
    }

    [Fact]
    public async Task RemoveAddress_RemovingDefault_PromotesAnotherAddressToDefault()
    {
        await AuthenticateAsync("address3@example.com");

        var firstResponse = await _client.PostAsJsonAsync("/api/identity/addresses", new { line1 = "A", line2 = (string?)null, city = "Paris", postalCode = "75002", country = "France" });
        var first = await firstResponse.Content.ReadFromJsonAsync<AddressResponse>();
        var secondResponse = await _client.PostAsJsonAsync("/api/identity/addresses", new { line1 = "B", line2 = (string?)null, city = "Lyon", postalCode = "69002", country = "France" });
        var second = await secondResponse.Content.ReadFromJsonAsync<AddressResponse>();

        var deleteResponse = await _client.DeleteAsync($"/api/identity/addresses/{first!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var addresses = await (await _client.GetAsync("/api/identity/addresses")).Content.ReadFromJsonAsync<List<AddressResponse>>();

        Assert.Single(addresses!);
        Assert.True(addresses!.Single(a => a.Id == second!.Id).IsDefault);
    }

    [Fact]
    public async Task UpdateProfile_WithValidDisplayName_UpdatesProfile()
    {
        await AuthenticateAsync("profile1@example.com");

        var response = await _client.PutAsJsonAsync("/api/identity/profile", new { displayName = "Nouveau Nom" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var profile = await (await _client.GetAsync("/api/identity/profile")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Nouveau Nom", profile.GetProperty("displayName").GetString());
    }

    [Fact]
    public async Task GetAddresses_WithoutToken_ReturnsUnauthorized()
    {
        using var anonymousClient = factory.CreateClient();

        var response = await anonymousClient.GetAsync("/api/identity/addresses");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Ces trois tests couvrent le scénario Gherkin "Adresse d'un autre client" de l'Issue #22.
    // Chaque endpoint de mutation d'adresse résout d'abord le User du token appelant, puis
    // cherche l'adresse UNIQUEMENT dans les adresses de ce User (User.FindAddress) - l'isolation
    // est donc déjà garantie par construction, ces tests le vérifient explicitement.
    [Fact]
    public async Task UpdateAddress_BelongingToAnotherClient_IsRejected()
    {
        var addressId = await CreateAddressAsync("owner-update@example.com");
        await AuthenticateAsync("intruder-update@example.com");

        var response = await _client.PutAsJsonAsync($"/api/identity/addresses/{addressId}", new
        {
            line1 = "Adresse usurpée",
            line2 = (string?)null,
            city = "Marseille",
            postalCode = "13001",
            country = "France"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RemoveAddress_BelongingToAnotherClient_IsRejected()
    {
        var addressId = await CreateAddressAsync("owner-remove@example.com");
        await AuthenticateAsync("intruder-remove@example.com");

        var response = await _client.DeleteAsync($"/api/identity/addresses/{addressId}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SetDefaultAddress_BelongingToAnotherClient_IsRejected()
    {
        var addressId = await CreateAddressAsync("owner-default@example.com");
        await AuthenticateAsync("intruder-default@example.com");

        var response = await _client.PostAsync($"/api/identity/addresses/{addressId}/default", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<Guid> CreateAddressAsync(string ownerEmail)
    {
        await AuthenticateAsync(ownerEmail);

        var response = await _client.PostAsJsonAsync("/api/identity/addresses", new
        {
            line1 = "1 rue du Propriétaire",
            line2 = (string?)null,
            city = "Paris",
            postalCode = "75001",
            country = "France"
        });

        var address = await response.Content.ReadFromJsonAsync<AddressResponse>();
        _client.DefaultRequestHeaders.Authorization = null;
        return address!.Id;
    }

    private async Task AuthenticateAsync(string email)
    {
        const string password = "MotDePasse123!";

        var registerResponse = await _client.PostAsJsonAsync("/api/identity/register", new { email, password, displayName = "Test" });
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var confirmationBody = factory.EmailSender.SentEmails.Last(e => e.To == email).Body;
        var confirmationPath = new Uri(Regex.Match(confirmationBody, @"http\S+").Value).PathAndQuery;
        var confirmResponse = await _client.GetAsync(confirmationPath);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        var loginResponse = await _client.PostAsJsonAsync("/api/identity/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var login = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = login.GetProperty("accessToken").GetString();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private sealed record AddressResponse(Guid Id, string Line1, string? Line2, string City, string PostalCode, string Country, bool IsDefault);
}
