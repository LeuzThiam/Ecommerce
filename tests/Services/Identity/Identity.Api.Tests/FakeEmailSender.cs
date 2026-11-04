using Identity.Application;

namespace Identity.Api.Tests;

// Remplace DevelopmentEmailSender dans les tests, pour pouvoir récupérer le token de
// confirmation/reset envoyé "par email" sans dépendre d'un vrai serveur SMTP ni du logging.
public sealed class FakeEmailSender : IEmailSender
{
    public List<(string To, string Subject, string Body)> SentEmails { get; } = [];

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        SentEmails.Add((to, subject, body));
        return Task.CompletedTask;
    }
}
