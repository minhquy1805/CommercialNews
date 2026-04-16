using System.ComponentModel.DataAnnotations;

namespace Identity.Application.Contracts.RefreshToken;

public sealed class RefreshTokenRequestDto
{
    [Required]
    [MaxLength(500)]
    public string RefreshToken { get; init; } = string.Empty;
}