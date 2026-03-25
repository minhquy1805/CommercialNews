namespace Authorization.Application.Contracts.Responses
{
    public sealed class ActivatePermissionResponseDto
    {
        public long PermissionId { get; init; }
        public bool IsActivated { get; init; }
        public bool WasAlreadyActivated { get; init; }
    }
}