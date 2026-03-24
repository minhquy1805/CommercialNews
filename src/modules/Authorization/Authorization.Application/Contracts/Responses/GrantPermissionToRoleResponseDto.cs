namespace Authorization.Application.Contracts.Responses
{
    public sealed class GrantPermissionToRoleResponseDto
    {
        public long RolePermissionId { get; init; }
        public long RoleId { get; init; }
        public long PermissionId { get; init; }

        public bool IsGranted { get; init; }
        public bool WasAlreadyGranted { get; init; }
    }
}

