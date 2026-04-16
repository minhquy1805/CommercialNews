using System.ComponentModel.DataAnnotations;

namespace Identity.Application.Contracts.ChangePassword;

public sealed class ChangePasswordRequestDto
{
    [Required]
    [MaxLength(200)]
    public string CurrentPassword { get; init; } = string.Empty;

    [Required]
    [MinLength(12)]
    [MaxLength(200)]
    public string NewPassword { get; init; } = string.Empty;
}