namespace Authorization.Application.Contracts.Responses
{
    public sealed class ActivateRoleResponseDto
    {
        public long RoleId { get; init; }
        public bool IsActivated { get; init; }
        public bool WasAlreadyActivated { get; init; }
    }
}