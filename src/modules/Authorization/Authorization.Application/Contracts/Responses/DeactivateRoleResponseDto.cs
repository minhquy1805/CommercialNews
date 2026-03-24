namespace Authorization.Application.Contracts.Responses
{
    public sealed class DeactivateRoleResponseDto
    {
        public long RoleId { get; init; }
        public bool IsDeactivated { get; init; }
        public bool WasAlreadyDeactivated { get; init; }
    }
}

