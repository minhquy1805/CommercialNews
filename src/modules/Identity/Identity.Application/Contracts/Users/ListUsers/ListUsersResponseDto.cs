namespace Identity.Application.Contracts.Users.ListUsers;

public sealed class ListUsersResponseDto
{
    public IReadOnlyList<UserListItemDto> Items { get; init; } =
        Array.Empty<UserListItemDto>();

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalItems { get; init; }
}