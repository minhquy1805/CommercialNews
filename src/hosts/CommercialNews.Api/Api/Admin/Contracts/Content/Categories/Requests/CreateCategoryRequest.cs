namespace CommercialNews.Api.Api.Admin.Contracts.Content.Categories.Requests;

public sealed class CreateCategoryRequest
{
    public long? ParentCategoryId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public bool IsActive { get; init; } = true;

    public int DisplayOrder { get; init; }
}