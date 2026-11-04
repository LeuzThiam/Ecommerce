using Identity.Domain.Common;

namespace Identity.Domain.Identity;

public sealed class Address
{
    public Guid Id { get; }
    public string Line1 { get; private set; }
    public string? Line2 { get; private set; }
    public string City { get; private set; }
    public string PostalCode { get; private set; }
    public string Country { get; private set; }
    public bool IsDefault { get; private set; }

    private Address(Guid id, string line1, string? line2, string city, string postalCode, string country, bool isDefault)
    {
        Id = id;
        Line1 = line1;
        Line2 = line2;
        City = city;
        PostalCode = postalCode;
        Country = country;
        IsDefault = isDefault;
    }

    internal static Address Create(string line1, string? line2, string city, string postalCode, string country)
    {
        Validate(line1, city, postalCode, country);

        return new Address(
            Guid.NewGuid(),
            line1.Trim(),
            string.IsNullOrWhiteSpace(line2) ? null : line2.Trim(),
            city.Trim(),
            postalCode.Trim(),
            country.Trim(),
            isDefault: false);
    }

    internal void Update(string line1, string? line2, string city, string postalCode, string country)
    {
        Validate(line1, city, postalCode, country);

        Line1 = line1.Trim();
        Line2 = string.IsNullOrWhiteSpace(line2) ? null : line2.Trim();
        City = city.Trim();
        PostalCode = postalCode.Trim();
        Country = country.Trim();
    }

    internal void MarkAsDefault() => IsDefault = true;

    internal void UnmarkAsDefault() => IsDefault = false;

    private static void Validate(string line1, string city, string postalCode, string country)
    {
        if (string.IsNullOrWhiteSpace(line1))
        {
            throw new DomainException("L'adresse (ligne 1) est requise.");
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            throw new DomainException("La ville est requise.");
        }

        if (string.IsNullOrWhiteSpace(postalCode))
        {
            throw new DomainException("Le code postal est requis.");
        }

        if (string.IsNullOrWhiteSpace(country))
        {
            throw new DomainException("Le pays est requis.");
        }
    }
}
