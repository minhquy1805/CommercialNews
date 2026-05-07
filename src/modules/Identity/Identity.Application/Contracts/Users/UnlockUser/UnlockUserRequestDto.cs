using System.ComponentModel.DataAnnotations;

namespace Identity.Application.Contracts.Users.UnlockUser;

public sealed class UnlockUserRequestDto
{
    public long UserId { get; init; }

    [MaxLength(500)]
    public string? Reason { get; init; }
}