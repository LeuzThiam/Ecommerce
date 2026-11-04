namespace Identity.Infrastructure;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "Ecommerce";
    public string Audience { get; set; } = "Ecommerce";
}
