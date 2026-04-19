namespace Authorization.Application.Contracts.Permissions;

public sealed class DeactivatePermissionResponseDto
{
    public long PermissionId { get; init; }
    public bool IsDeactivated { get; init; }
    public bool WasAlreadyDeactivated { get; init; }
}