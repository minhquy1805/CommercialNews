namespace CommercialNews.Api.Api.Admin.Contracts.Authorization.UserRoles.Responses;

public sealed class EffectivePermissionItemHttpResponse
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
}