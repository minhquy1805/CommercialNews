using System.ComponentModel.DataAnnotations;

namespace Identity.Application.Contracts.Users.MarkEmailVerified;

public sealed class MarkEmailVerifiedRequestDto
{
    public long UserId { get; init; }

    [MaxLength(500)]
    public string? Reason { get; init; }
}