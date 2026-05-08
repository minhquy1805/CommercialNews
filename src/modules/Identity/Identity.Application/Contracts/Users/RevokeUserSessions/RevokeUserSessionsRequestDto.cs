using System.ComponentModel.DataAnnotations;

namespace Identity.Application.Contracts.Users.RevokeUserSessions;

public sealed class RevokeUserSessionsRequestDto
{
    public long UserId { get; init; }

    [MaxLength(500)]
    public string? Reason { get; init; }
}