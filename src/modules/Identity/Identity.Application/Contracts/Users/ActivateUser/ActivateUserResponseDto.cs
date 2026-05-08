namespace Identity.Application.Contracts.Users.ActivateUser;

public sealed class ActivateUserResponseDto
{
    public long UserId { get; init; }

    public string PublicId { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public bool Activated { get; init; }

    public DateTime ActivatedAtUtc { get; init; }
}