namespace Authorization.Application.Contracts.Permissions;

public sealed class ActivatePermissionResponseDto
{
    public long PermissionId { get; init; }
    public bool IsActivated { get; init; }
    public bool WasAlreadyActivated { get; init; }
}