namespace Authorization.Application.Models.QueryModels;

public sealed class RoleListResultItem
{
    public long RoleId { get; init; }
    public string PublicId { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;
    public string NameNormalized { get; init; } = string.Empty;
    public string? Description { get; init; }

    public bool IsSystem { get; init; }
    public bool IsActive { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public long? CreatedByUserId { get; init; }
    public long? UpdatedByUserId { get; init; }
}