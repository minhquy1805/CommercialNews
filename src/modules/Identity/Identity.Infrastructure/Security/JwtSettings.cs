namespace Identity.Infrastructure.Security;

public sealed class JwtSettings
{
    public const string SectionName = "Identity:Jwt";

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public int AccessTokenLifetimeMinutes { get; init; } = 15;
}