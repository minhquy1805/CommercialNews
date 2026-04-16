using System.ComponentModel.DataAnnotations;

namespace Identity.Application.Contracts.RegisterUser;

public sealed class RegisterUserRequestDto
{
    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MinLength(12)]
    [MaxLength(200)]
    public string Password { get; init; } = string.Empty;

    [MaxLength(200)]
    public string? FullName { get; init; }
}