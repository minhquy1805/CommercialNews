namespace Identity.Application.Ports.Services.Models
{
    public sealed class AccessTokenResult
    {
        public string AccessToken { get; init; } = string.Empty;
        public DateTime ExpiresAtUtc { get; init; }
    }
}