using Ordering.Domain.Common;

namespace Ordering.Domain;

// Garde une trace de chaque événement webhook déjà traité (identifié par l'Id unique que
// Stripe attribue à chaque événement). Si le même webhook est reçu deux fois — Stripe
// garantit "au moins une fois", jamais "exactement une fois" — on le détecte ici et on ne
// retraite rien.
public sealed class ProcessedWebhookEvent
{
    public string EventId { get; }
    public DateTime ProcessedAtUtc { get; }

    private ProcessedWebhookEvent(string eventId, DateTime processedAtUtc)
    {
        EventId = eventId;
        ProcessedAtUtc = processedAtUtc;
    }

    public static ProcessedWebhookEvent Create(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            throw new DomainException("L'identifiant de l'événement est requis.");
        }

        return new ProcessedWebhookEvent(eventId, DateTime.UtcNow);
    }
}
