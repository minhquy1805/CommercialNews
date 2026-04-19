namespace Authorization.Application.Contracts.Permissions;

public sealed class UpdatePermissionRequestDto
{
    public long PermissionId { get; init; }
    public string Key { get; init; } = string.Empty;
    public string? Module { get; init; }
    public string? Action { get; init; }
    public string? Description { get; init; }
}