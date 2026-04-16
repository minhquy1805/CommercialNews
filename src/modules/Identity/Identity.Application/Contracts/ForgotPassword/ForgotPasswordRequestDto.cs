using System.ComponentModel.DataAnnotations;

namespace Identity.Application.Contracts.ForgotPassword;

public sealed class ForgotPasswordRequestDto
{
    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string Email { get; init; } = string.Empty;
}