namespace Notifications.Application.Configuration;

public sealed class EmailDeliveryOptions
{
    public const string SectionName = "Notifications:Delivery";

    public string Provider { get; init; } = "smtp";

    public byte VerificationEmailPriority { get; init; } = 1;

    public byte PasswordResetPriority { get; init; } = 1;

    public byte PasswordChangedPriority { get; init; } = 3;

    public byte EmailVerifiedPriority { get; init; } = 5;

    public int MaxAttemptCount { get; init; } = 5;

    public int InitialRetryDelaySeconds { get; init; } = 30;

    public int MaxRetryDelaySeconds { get; init; } = 900;

    public string VerificationEmailUrlTemplate { get; init; } =
        "http://localhost:3000/verify-email?token={token}";

    public string ResetPasswordUrlTemplate { get; init; } =
        "http://localhost:3000/reset-password?token={token}";
}