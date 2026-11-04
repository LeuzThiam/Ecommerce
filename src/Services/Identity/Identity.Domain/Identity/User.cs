using Identity.Domain.Common;

namespace Identity.Domain.Identity;

public sealed class User
{
    private readonly List<Address> _addresses = [];

    public Guid Id { get; }
    public Email Email { get; }
    public string DisplayName { get; private set; }
    public DateTime CreatedAtUtc { get; }
    public IReadOnlyList<Address> Addresses => _addresses;

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

    public void UpdateDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new DomainException("Le nom d'affichage est requis.");
        }

        DisplayName = displayName.Trim();
    }

    public Address AddAddress(string line1, string? line2, string city, string postalCode, string country)
    {
        var address = Address.Create(line1, line2, city, postalCode, country);

        // La première adresse ajoutée devient automatiquement l'adresse par défaut.
        if (_addresses.Count == 0)
        {
            address.MarkAsDefault();
        }

        _addresses.Add(address);
        return address;
    }

    public void UpdateAddress(Guid addressId, string line1, string? line2, string city, string postalCode, string country)
    {
        var address = FindAddress(addressId);
        address.Update(line1, line2, city, postalCode, country);
    }

    public void RemoveAddress(Guid addressId)
    {
        var address = FindAddress(addressId);
        _addresses.Remove(address);

        // Si l'adresse par défaut est supprimée, une autre adresse restante (s'il y en a) la
        // remplace automatiquement — on ne laisse jamais le client sans adresse par défaut tant
        // qu'il lui en reste au moins une.
        if (address.IsDefault && _addresses.Count > 0)
        {
            _addresses[0].MarkAsDefault();
        }
    }

    public void SetDefaultAddress(Guid addressId)
    {
        var address = FindAddress(addressId);

        foreach (var other in _addresses)
        {
            other.UnmarkAsDefault();
        }

        address.MarkAsDefault();
    }

    private Address FindAddress(Guid addressId) =>
        _addresses.FirstOrDefault(a => a.Id == addressId)
            ?? throw new DomainException("Adresse introuvable.");
}