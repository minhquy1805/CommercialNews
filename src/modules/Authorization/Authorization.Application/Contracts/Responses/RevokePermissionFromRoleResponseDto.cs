namespace Authorization.Application.Contracts.Responses
{
    public sealed class RevokePermissionFromRoleResponseDto
    {
        public long RoleId { get; init; }
        public long PermissionId { get; init; }

        public bool IsRevoked { get; init; }
        public bool WasAlreadyRevoked { get; init; }
    }   
}