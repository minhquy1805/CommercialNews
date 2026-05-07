namespace Identity.Application.Contracts.Users.UnlockUser;

public sealed class UnlockUserResponseDto
{
    public long UserId { get; init; }

    public string PublicId { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public bool Unlocked { get; init; }

    public DateTime UnlockedAtUtc { get; init; }
}