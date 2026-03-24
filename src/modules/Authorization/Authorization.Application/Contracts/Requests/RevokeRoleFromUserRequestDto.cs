namespace Authorization.Application.Contracts.Requests
{
    public sealed class RevokeRoleFromUserRequestDto
    {
        public long UserId { get; init; }
        public long RoleId { get; init; }
    }
}

