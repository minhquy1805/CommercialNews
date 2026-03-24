namespace Authorization.Application.Contracts.Responses
{
    public sealed class GetUserRolesResponseDto
    {
        public long UserId { get; init; }
        public IReadOnlyList<UserRoleItemDto> Roles { get; init; } = Array.Empty<UserRoleItemDto>();
    }
}

