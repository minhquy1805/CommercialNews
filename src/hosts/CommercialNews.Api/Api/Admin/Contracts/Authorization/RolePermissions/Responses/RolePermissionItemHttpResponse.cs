namespace CommercialNews.Api.Api.Admin.Contracts.Authorization.RolePermissions.Responses;

public sealed class RolePermissionItemHttpResponse
{
    public long PermissionId { get; init; }
    public string PublicId { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public string KeyNormalized { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Module { get; init; }
    public string? Action { get; init; }
    public bool IsSystem { get; init; }
    public bool IsActive { get; init; }
    public DateTime GrantedAt { get; init; }
    public long? GrantedByUserId { get; init; }
}