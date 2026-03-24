namespace Authorization.Application.Contracts.Requests
{ 
    public sealed class GetRoleUsersRequestDto
    {
        public long RoleId { get; init; }
    }
}
