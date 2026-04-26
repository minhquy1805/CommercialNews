namespace Identity.Infrastructure.Configuration;

public sealed class DefaultAdminSettings
{
    public const string SectionName = "Identity:DefaultAdmin";

    public bool Enabled { get; init; } = false;

    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string? FullName { get; init; }
}