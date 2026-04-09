namespace Identity.Infrastructure.Services;

public sealed class IdentityNotificationOutboxOptions
{
    public string VerificationBaseUrl { get; init; } = string.Empty;
}