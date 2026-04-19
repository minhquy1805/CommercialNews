namespace Authorization.Application.Contracts.Permissions;

public sealed class UpdatePermissionResponseDto
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

    public DateTime UpdatedAt { get; init; }
    public long? UpdatedByUserId { get; init; }
}