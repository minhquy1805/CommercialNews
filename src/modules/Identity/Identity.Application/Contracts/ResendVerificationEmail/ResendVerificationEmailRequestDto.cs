using System.ComponentModel.DataAnnotations;

namespace Identity.Application.Contracts.ResendVerificationEmail;

public sealed class ResendVerificationEmailRequestDto
{
    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string Email { get; init; } = string.Empty;
}