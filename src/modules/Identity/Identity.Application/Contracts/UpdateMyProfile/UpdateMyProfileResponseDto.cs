namespace Identity.Application.Contracts.UpdateMyProfile;

public sealed class UpdateMyProfileResponseDto
{
    public long UserId { get; init; }
    public string PublicId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? FullName { get; init; }
    public string? AvatarUrl { get; init; }
    public bool IsEmailVerified { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime UpdatedAt { get; init; }
}