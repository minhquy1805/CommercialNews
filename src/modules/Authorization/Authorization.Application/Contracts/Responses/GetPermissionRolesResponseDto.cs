namespace Authorization.Application.Contracts.Responses
{
    public sealed class GetPermissionRolesResponseDto
    {
        public long PermissionId { get; init; }
        public IReadOnlyList<PermissionRoleItemDto> Roles { get; init; } = Array.Empty<PermissionRoleItemDto>();
    }
}

