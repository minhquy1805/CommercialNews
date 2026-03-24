namespace Authorization.Application.Contracts.Requests;

public sealed class CreatePermissionRequestDto
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Module { get; init; }
    public bool IsSystem { get; init; }
}