using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Identity.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Api.Tests;

public class AdminUserManagementTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetAllUsersAdmin_AsAdmin_ReturnsUsersWithTheirRoles()
    {
        var clientEmail = "user-list-client@example.com";
        await RegisterAndConfirmAsync(clientEmail);

        var adminEmail = "user-list-admin@example.com";
        await RegisterAndConfirmAsync(adminEmail);
        await PromoteToAdminDirectlyAsync(adminEmail);
        await AuthenticateAsync(adminEmail);

        var response = await _client.GetAsync("/api/identity/admin/users");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var users = await response.Content.ReadFromJsonAsync<List<UserAdminResponse>>();

        var client = users!.Single(u => u.Email == clientEmail);
        Assert.Contains("Client", client.Roles);
        Assert.DoesNotContain("Administrator", client.Roles);

        var admin = users!.Single(u => u.Email == adminEmail);
        Assert.Contains("Administrator", admin.Roles);
    }

    [Fact]
    public async Task GetAllUsersAdmin_AsClient_ReturnsForbidden()
    {
        await AuthenticateAsync("user-list-forbidden@example.com");

        var response = await _client.GetAsync("/api/identity/admin/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAllUsersAdmin_WithoutToken_ReturnsUnauthorized()
    {
        using var anonymousClient = factory.CreateClient();

        var response = await anonymousClient.GetAsync("/api/identity/admin/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PromoteUser_AsAdmin_GrantsAdministratorRole()
    {
        var targetEmail = "promote-target@example.com";
        var targetId = await RegisterAndConfirmAsync(targetEmail);

        await PromoteToAdminDirectlyAsync("promote-admin@example.com", register: true);
        await AuthenticateAsync("promote-admin@example.com");

        var response = await _client.PostAsync($"/api/identity/admin/users/{targetId}/promote", null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var users = await (await _client.GetAsync("/api/identity/admin/users")).Content.ReadFromJsonAsync<List<UserAdminResponse>>();
        Assert.Contains("Administrator", users!.Single(u => u.Id == targetId).Roles);
    }

    [Fact]
    public async Task DemoteUser_AsAdmin_RemovesAdministratorRole()
    {
        var targetEmail = "demote-target@example.com";
        var targetId = await RegisterAndConfirmAsync(targetEmail);
        await PromoteToAdminDirectlyAsync(targetEmail);

        await PromoteToAdminDirectlyAsync("demote-admin@example.com", register: true);
        await AuthenticateAsync("demote-admin@example.com");

        var response = await _client.PostAsync($"/api/identity/admin/users/{targetId}/demote", null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var users = await (await _client.GetAsync("/api/identity/admin/users")).Content.ReadFromJsonAsync<List<UserAdminResponse>>();
        Assert.DoesNotContain("Administrator", users!.Single(u => u.Id == targetId).Roles);
    }

    [Fact]
    public async Task DemoteUser_Self_IsRejected()
    {
        var selfEmail = "demote-self@example.com";
        var selfId = await PromoteToAdminDirectlyAsync(selfEmail, register: true);
        await AuthenticateAsync(selfEmail);

        var response = await _client.PostAsync($"/api/identity/admin/users/{selfId}/demote", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PromoteUser_AsClient_ReturnsForbidden()
    {
        var targetId = await RegisterAndConfirmAsync("promote-forbidden-target@example.com");
        await AuthenticateAsync("promote-forbidden-caller@example.com");

        var response = await _client.PostAsync($"/api/identity/admin/users/{targetId}/promote", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<Guid> RegisterAndConfirmAsync(string email)
    {
        const string password = "MotDePasse123!";

        var registerResponse = await _client.PostAsJsonAsync("/api/identity/register", new { email, password, displayName = "Test" });
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        var registered = await registerResponse.Content.ReadFromJsonAsync<JsonElement>();
        var userId = registered.GetProperty("id").GetGuid();

        var confirmationBody = factory.EmailSender.SentEmails.Last(e => e.To == email).Body;
        var confirmationPath = new Uri(Regex.Match(confirmationBody, @"http\S+").Value).PathAndQuery;
        await _client.GetAsync(confirmationPath);

        return userId;
    }

    // Reproduit, via l'API Identity elle-même (UserManager), ce que j'ai fait manuellement en SQL
    // sur la vraie base pour créer le tout premier admin - c'est la seule façon d'obtenir un admin
    // de test, exactement comme en production.
    private async Task<Guid> PromoteToAdminDirectlyAsync(string email, bool register = false)
    {
        var userId = register ? await RegisterAndConfirmAsync(email) : Guid.Empty;

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var appUser = await userManager.FindByEmailAsync(email);
        await userManager.AddToRoleAsync(appUser!, "Administrator");

        return register ? userId : appUser!.Id;
    }

    private async Task AuthenticateAsync(string email)
    {
        const string password = "MotDePasse123!";

        if ((await _client.PostAsJsonAsync("/api/identity/login", new { email, password })).StatusCode != HttpStatusCode.OK)
        {
            await RegisterAndConfirmAsync(email);
        }

        var loginResponse = await _client.PostAsJsonAsync("/api/identity/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var login = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = login.GetProperty("accessToken").GetString();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private sealed record UserAdminResponse(Guid Id, string Email, string DisplayName, DateTime CreatedAtUtc, bool EmailConfirmed, List<string> Roles);
}
