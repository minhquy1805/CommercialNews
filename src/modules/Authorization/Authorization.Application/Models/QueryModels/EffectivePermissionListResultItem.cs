namespace Authorization.Application.Models.QueryModels;

public sealed class EffectivePermissionListResultItem
{
    public long PermissionId { get; init; }
    public string PublicId { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;
    public string NameNormalized { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Module { get; init; }

    public bool IsSystem { get; init; }
    public bool IsActive { get; init; }
}