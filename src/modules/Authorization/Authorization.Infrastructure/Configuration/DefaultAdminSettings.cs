namespace Authorization.Infrastructure.Configuration;

public sealed class DefaultAdminSettings
{
    public const string SectionName = "Authorization:DefaultAdmin";

    public bool Enabled { get; init; } = false;

    public string Email { get; init; } = string.Empty;
}