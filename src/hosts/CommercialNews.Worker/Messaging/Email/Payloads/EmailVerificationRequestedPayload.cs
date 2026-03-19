namespace CommercialNews.Worker.Messaging.Email.Payloads
{
    public sealed class EmailVerificationRequestedPayload
    {
        public long UserId { get; init; }
        public string PublicId { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string RawToken { get; init; } = string.Empty;
    }
}

