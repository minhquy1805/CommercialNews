namespace Notifications.Application.Models.OutboxPayloads;

public sealed class IdentityTemplateVariables
{
    public string? UserName { get; init; }

    public string? VerificationUrl { get; init; }

    public string? ResetUrl { get; init; }

    public string? ChangedAtUtc { get; init; }

    public bool IsEmpty()
    {
        return string.IsNullOrWhiteSpace(UserName)
            && string.IsNullOrWhiteSpace(VerificationUrl)
            && string.IsNullOrWhiteSpace(ResetUrl)
            && string.IsNullOrWhiteSpace(ChangedAtUtc);
    }
}