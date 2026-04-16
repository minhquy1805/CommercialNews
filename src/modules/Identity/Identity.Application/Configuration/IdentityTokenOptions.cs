namespace Identity.Application.Configuration;

public sealed class IdentityTokenOptions
{
    public const string SectionName = "Identity:Tokens";

    public int EmailVerificationTokenLifetimeHours { get; init; } = 24;

    public int PasswordResetTokenLifetimeHours { get; init; } = 1;

    public int RefreshTokenLifetimeDays { get; init; } = 30;
}