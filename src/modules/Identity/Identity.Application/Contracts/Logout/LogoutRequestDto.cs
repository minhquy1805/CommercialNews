using System.ComponentModel.DataAnnotations;

namespace Identity.Application.Contracts.Logout;

public sealed class LogoutRequestDto
{
    [Required]
    [MaxLength(500)]
    public string RefreshToken { get; init; } = string.Empty;
}