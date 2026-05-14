namespace CommercialNews.Api.Api.Admin.Contracts.Content.Categories.Responses;

public sealed class UpdateCategoryResponse
{
    public long CategoryId { get; init; }
    public long? ParentCategoryId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string NameNormalized { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public int DisplayOrder { get; init; }
    public long Version { get; init; }
    public DateTime UpdatedAt { get; init; }
}
