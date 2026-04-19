namespace Authorization.Application.Contracts.Permissions;

public sealed class CreatePermissionResponseDto
{
    public long PermissionId { get; init; }
    public string PublicId { get; init; } = string.Empty;

    public string Key { get; init; } = string.Empty;
    public string KeyNormalized { get; init; } = string.Empty;
    public string? Module { get; init; }
    public string? Action { get; init; }
    public string? Description { get; init; }

    public bool IsSystem { get; init; }
    public bool IsActive { get; init; }

    public DateTime CreatedAt { get; init; }
    public long? CreatedByUserId { get; init; }
}