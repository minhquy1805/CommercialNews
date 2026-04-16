using System.ComponentModel.DataAnnotations;

namespace Identity.Application.Contracts.LoginUser;

public sealed class LoginUserRequestDto
{
    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Password { get; init; } = string.Empty;
}