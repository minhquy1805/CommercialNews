namespace Identity.Application.Contracts
{
    public static class IdentityOutboxEventTypes
    {
        public const string EmailVerificationRequested = "identity.email-verification.requested";
        public const string PasswordResetRequested = "identity.password-reset.requested";
    }
}