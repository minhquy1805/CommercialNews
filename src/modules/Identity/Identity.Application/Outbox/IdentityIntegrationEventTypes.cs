namespace Identity.Application.Outbox;

public static class IdentityIntegrationEventTypes
{
    public const string UserRegistered =
        "identity.user_registered";

    public const string VerificationEmailRequested =
        "identity.verification_email_requested";

    public const string PasswordResetRequested =
        "identity.password_reset_requested";

    public const string PasswordChanged =
        "identity.password_changed";

    public const string EmailVerified =
        "identity.email_verified";

    public const string UserActivated =
        "identity.user_activated";

    public const string UserDisabled =
        "identity.user_disabled";

    public const string UserLocked =
        "identity.user_locked";

    public const string UserUnlocked =
        "identity.user_unlocked";

    public const string EmailMarkedVerified =
        "identity.email_marked_verified";

    public const string UserSessionsRevoked =
        "identity.user_sessions_revoked";
}
