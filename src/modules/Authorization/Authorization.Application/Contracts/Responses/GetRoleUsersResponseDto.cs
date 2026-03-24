namespace Authorization.Application.Contracts.Responses
{
    public sealed class GetRoleUsersResponseDto
    {
        public long RoleId { get; init; }
        public IReadOnlyList<RoleUserItemDto> Users { get; init; } = Array.Empty<RoleUserItemDto>();
    }
}