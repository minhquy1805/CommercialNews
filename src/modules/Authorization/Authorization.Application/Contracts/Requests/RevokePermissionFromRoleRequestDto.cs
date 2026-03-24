namespace Authorization.Application.Contracts.Requests
{
    public sealed class RevokePermissionFromRoleRequestDto
    {
        public long RoleId { get; init; }
        public long PermissionId { get; init; }
    }
}

