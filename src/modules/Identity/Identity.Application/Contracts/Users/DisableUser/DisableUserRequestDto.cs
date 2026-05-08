using System.ComponentModel.DataAnnotations;

namespace Identity.Application.Contracts.Users.DisableUser;

public sealed class DisableUserRequestDto
{
    public long UserId { get; init; }

    [MaxLength(500)]
    public string? Reason { get; init; }

    public bool RevokeSessions { get; init; } = true;
}