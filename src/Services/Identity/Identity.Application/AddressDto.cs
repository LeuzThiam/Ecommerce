namespace Identity.Application;

public sealed record AddressDto(
    Guid Id,
    string Line1,
    string? Line2,
    string City,
    string PostalCode,
    string Country,
    bool IsDefault);
