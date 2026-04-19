namespace CommercialNews.Api.Api.Admin.Contracts.Authorization.Roles.Responses;

public sealed class UpdateRoleHttpResponse
{
    public long RoleId { get; init; }
    public string PublicId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string NameNormalized { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public bool IsSystem { get; init; }
    public bool IsActive { get; init; }
    public DateTime UpdatedAt { get; init; }
    public long? UpdatedByUserId { get; init; }
}