using System.ComponentModel.DataAnnotations;

namespace Identity.Application.Contracts.VerifyEmail;

public sealed class VerifyEmailRequestDto
{
    [Required]
    [MaxLength(500)]
    public string Token { get; init; } = string.Empty;
}