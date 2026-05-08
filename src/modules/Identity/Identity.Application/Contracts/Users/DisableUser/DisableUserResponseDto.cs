namespace Identity.Application.Contracts.Users.DisableUser;

public sealed class DisableUserResponseDto
{
    public long UserId { get; init; }

    public string PublicId { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public bool Disabled { get; init; }

    public bool SessionsRevoked { get; init; }

    public int RevokedSessionCount { get; init; }

    public DateTime DisabledAtUtc { get; init; }
}