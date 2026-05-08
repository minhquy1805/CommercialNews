using System.ComponentModel.DataAnnotations;

namespace Identity.Application.Contracts.Users.LockUser;

public sealed class LockUserRequestDto
{
    public long UserId { get; init; }

    public DateTime LockedUntilUtc { get; init; }

    [MaxLength(500)]
    public string? Reason { get; init; }

    public bool RevokeSessions { get; init; } = true;
}