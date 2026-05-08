namespace Identity.Application.Contracts.Users.MarkEmailVerified;

public sealed class MarkEmailVerifiedResponseDto
{
    public long UserId { get; init; }

    public string PublicId { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public bool IsEmailVerified { get; init; }

    public bool WasAlreadyVerified { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTime MarkedVerifiedAtUtc { get; init; }
}