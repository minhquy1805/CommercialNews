using System.ComponentModel.DataAnnotations;

namespace Identity.Application.Contracts.Users.ActivateUser;

public sealed class ActivateUserRequestDto
{
    public long UserId { get; init; }

    [MaxLength(500)]
    public string? Reason { get; init; }
}