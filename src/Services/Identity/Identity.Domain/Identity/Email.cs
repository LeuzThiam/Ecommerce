using Identity.Domain.Common;

namespace Identity.Domain.Identity;

public sealed record Email
{
    public string Value { get; }
    private Email(string value) => Value = value;

    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("L'adresse email ne peut pas être vide.");
        }
        
        string normalizeEmail = value.Trim().ToLowerInvariant();

        if (!normalizeEmail.Contains("@"))
        {
            throw new DomainException("L'adresse email n'est pas valide.");
        }
        
        return new Email(normalizeEmail);
    }

    public override string ToString() => Value;
}