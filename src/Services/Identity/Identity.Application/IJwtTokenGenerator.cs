namespace Identity.Application;

public interface IJwtTokenGenerator
{
    string GenerateToken(Guid userId, string email, IReadOnlyList<string> roles);
}
