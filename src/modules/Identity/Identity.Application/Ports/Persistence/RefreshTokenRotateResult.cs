namespace Identity.Application.Ports.Persistence;

public sealed class RefreshTokenRotateResult
{
    public long NewRefreshTokenId { get; init; }

    public long UserId { get; init; }
}
