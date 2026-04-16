using System.ComponentModel.DataAnnotations;

namespace Identity.Application.Contracts.ResetPassword;

public sealed class ResetPasswordRequestDto
{
    [Required]
    [MaxLength(500)]
    public string Token { get; init; } = string.Empty;

    [Required]
    [MinLength(12)]
    [MaxLength(200)]
    public string NewPassword { get; init; } = string.Empty;
}