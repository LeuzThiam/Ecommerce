using Identity.Application;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Emailing;

// Écrit l'email dans les logs au lieu de l'envoyer réellement - utilisé en Development et
// Testing, pour pouvoir vérifier/tester les flux de confirmation et de reset sans serveur SMTP.
public sealed class DevelopmentEmailSender(ILogger<DevelopmentEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        logger.LogInformation("Email (dev) à {To} - Sujet : {Subject}\n{Body}", to, subject, body);
        return Task.CompletedTask;
    }
}
