using System.ComponentModel.DataAnnotations;

namespace Identity.Application.Contracts.UpdateMyProfile;

public sealed class UpdateMyProfileRequestDto
{
    [MaxLength(200)]
    public string? FullName { get; init; }

    [MaxLength(800)]
    public string? AvatarUrl { get; init; }
}