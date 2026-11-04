using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace Identity.Api.Tests;

public class PasswordResetTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task ForgotPassword_WithUnknownEmail_StillReturnsOk()
    {
        var response = await _client.PostAsJsonAsync("/api/identity/forgot-password", new { email = "ne-existe-pas@example.com" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_AllowsLoginWithNewPassword()
    {
        const string email = "reset-password@example.com";
        await RegisterAndConfirmAsync(email);

        var forgotResponse = await _client.PostAsJsonAsync("/api/identity/forgot-password", new { email });
        Assert.Equal(HttpStatusCode.OK, forgotResponse.StatusCode);

        var resetToken = ExtractToken(factory.EmailSender.SentEmails.Last(e => e.To == email).Body);

        var resetResponse = await _client.PostAsJsonAsync("/api/identity/reset-password", new
        {
            email,
            token = resetToken,
            newPassword = "NouveauMotDePasse456!"
        });
        Assert.Equal(HttpStatusCode.OK, resetResponse.StatusCode);

        var loginWithOldPassword = await _client.PostAsJsonAsync("/api/identity/login", new { email, password = "MotDePasse123!" });
        Assert.Equal(HttpStatusCode.Unauthorized, loginWithOldPassword.StatusCode);

        var loginWithNewPassword = await _client.PostAsJsonAsync("/api/identity/login", new { email, password = "NouveauMotDePasse456!" });
        Assert.Equal(HttpStatusCode.OK, loginWithNewPassword.StatusCode);
    }

    private async Task RegisterAndConfirmAsync(string email)
    {
        var registerResponse = await _client.PostAsJsonAsync("/api/identity/register", new
        {
            email,
            password = "MotDePasse123!",
            displayName = "Test"
        });
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var confirmationBody = factory.EmailSender.SentEmails.Last(e => e.To == email).Body;
        var confirmationPath = new Uri(Regex.Match(confirmationBody, @"http\S+").Value).PathAndQuery;

        var confirmResponse = await _client.GetAsync(confirmationPath);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
    }

    private static string ExtractToken(string emailBody) => emailBody.TrimEnd().Split('\n').Last().Trim();
}
