namespace Identity.Application.Outbox;

public static class IdentityIntegrationEventTypes
{
    public const string VerificationEmailRequested =
        "identity.verification_email_requested";

    public const string PasswordResetRequested =
        "identity.password_reset_requested";

    public const string PasswordChanged =
        "identity.password_changed";

    public const string EmailVerified =
        "identity.email_verified";
}