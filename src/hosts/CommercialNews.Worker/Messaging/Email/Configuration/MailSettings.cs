namespace CommercialNews.Worker.Messaging.Email.Configuration
{
    public sealed class MailSettings
    {
        public string Host { get; init; } = string.Empty;
        public int Port { get; init; }
        public bool UseSsl { get; init; }
        public string UserName { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public string FromName { get; init; } = string.Empty;
        public string FromEmail { get; init; } = string.Empty;
    }
}

