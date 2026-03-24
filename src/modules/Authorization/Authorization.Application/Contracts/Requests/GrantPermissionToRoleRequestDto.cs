namespace Authorization.Application.Contracts.Requests
{
   public sealed class GrantPermissionToRoleRequestDto
    {
        public long RoleId { get; init; }
        public long PermissionId { get; init; }
    } 
}