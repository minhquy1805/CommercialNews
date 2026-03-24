namespace Authorization.Application.Contracts.Responses
{
    public sealed class GetRolePermissionsResponseDto
    {
        public long RoleId { get; init; }
        public IReadOnlyList<RolePermissionItemDto> Permissions { get; init; } = Array.Empty<RolePermissionItemDto>();
    }
}

