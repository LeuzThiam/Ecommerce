using Identity.Domain.Common;

namespace Identity.Domain.Identity;

public sealed class User
{
    public Guid Id { get; }
    public Email Email { get; }
    public string DisplayName { get; private set; }
    public DateTime CreatedAtUtc { get; }

    private User(Guid id, Email email, string displayName, DateTime createdAtUtc)
    {
        Id = id;
        Email = email;
        DisplayName = displayName;
        CreatedAtUtc = createdAtUtc;
    }

    public static User Register(string email, string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new DomainException("Le nom d'affichage est requis.");
        }

        Email validatedEmail = Email.Create(email);

        return new User(
            Guid.NewGuid(),
            validatedEmail,
            displayName.Trim(),
            DateTime.UtcNow
        );
    }

}