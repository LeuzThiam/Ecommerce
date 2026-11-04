using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace Identity.Api.Tests;

public class EmailConfirmationTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Login_WithUnconfirmedEmail_ReturnsBadRequest()
    {
        const string email = "unconfirmed@example.com";
        await RegisterAsync(email);

        var response = await _client.PostAsJsonAsync("/api/identity/login", new { email, password = "MotDePasse123!" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmEmail_WithValidToken_AllowsLogin()
    {
        const string email = "confirmed@example.com";
        await RegisterAsync(email);

        var confirmationPath = ExtractLinkPath(factory.EmailSender.SentEmails.Last(e => e.To == email).Body);
        var confirmResponse = await _client.GetAsync(confirmationPath);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        var loginResponse = await _client.PostAsJsonAsync("/api/identity/login", new { email, password = "MotDePasse123!" });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    [Fact]
    public async Task ConfirmEmail_WithInvalidToken_ReturnsBadRequest()
    {
        const string email = "invalid-confirmation@example.com";
        await RegisterAsync(email);

        var confirmationPath = ExtractLinkPath(factory.EmailSender.SentEmails.Last(e => e.To == email).Body);
        var tamperedPath = confirmationPath[..^4] + "0000";

        var response = await _client.GetAsync(tamperedPath);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task RegisterAsync(string email)
    {
        var response = await _client.PostAsJsonAsync("/api/identity/register", new
        {
            email,
            password = "MotDePasse123!",
            displayName = "Test"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static string ExtractLinkPath(string emailBody)
    {
        var match = Regex.Match(emailBody, @"http\S+");
        return new Uri(match.Value).PathAndQuery;
    }
}
