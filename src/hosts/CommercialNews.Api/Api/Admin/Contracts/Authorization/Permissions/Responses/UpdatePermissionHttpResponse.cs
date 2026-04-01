namespace CommercialNews.Api.Api.Admin.Contracts.Authorization.Permissions.Responses;

public sealed class UpdatePermissionHttpResponse
{
    public long PermissionId { get; init; }
    public string PublicId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string NameNormalized { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Module { get; init; }
    public bool IsSystem { get; init; }
    public bool IsActive { get; init; }
    public DateTime UpdatedAt { get; init; }
    public long? UpdatedByUserId { get; init; }
}