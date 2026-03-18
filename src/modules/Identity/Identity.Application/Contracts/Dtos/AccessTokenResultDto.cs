namespace Identity.Application.Contracts.Dtos
{
    public sealed class AccessTokenResultDto
    {
        public string AccessToken { get; init; } = string.Empty;
        public DateTime ExpiresAtUtc { get; init; }
    }
}
