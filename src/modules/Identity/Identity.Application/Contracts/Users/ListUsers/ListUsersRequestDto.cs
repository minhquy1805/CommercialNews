using System.ComponentModel.DataAnnotations;

namespace Identity.Application.Contracts.Users.ListUsers;

public sealed class ListUsersRequestDto
{
    public DateTime? FromCreatedAt { get; init; }

    public DateTime? ToCreatedAt { get; init; }

    [MaxLength(20)]
    public string? Status { get; init; }

    public bool? IsEmailVerified { get; init; }

    [MaxLength(320)]
    public string? Query { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}