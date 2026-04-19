namespace Authorization.Application.Contracts.Permissions;

public sealed class CreatePermissionRequestDto
{
    public string Key { get; init; } = string.Empty;
    public string? Module { get; init; }
    public string? Action { get; init; }
    public string? Description { get; init; }
    public bool IsSystem { get; init; }
}