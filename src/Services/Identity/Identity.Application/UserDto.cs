

namespace Identity.Application;

public sealed record UserDto(
    Guid Id,
    string Email,
    string DisplayName,
    DateTime CreatedAtUtc
    );

public sealed record UserAdminDto(
    Guid Id,
    string Email,
    string DisplayName,
    DateTime CreatedAtUtc,
    bool EmailConfirmed,
    IReadOnlyList<string> Roles);