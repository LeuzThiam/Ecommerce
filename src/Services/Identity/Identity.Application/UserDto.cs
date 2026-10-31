

namespace Identity.Application;

public sealed record UserDto(
    Guid Id,
    string Email,
    string DisplayName,
    DateTime CreatedAtUtc
    );