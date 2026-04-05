namespace Seo.Application.Contracts.SlugRegistry.Requests;

public sealed class GetSlugRegistryListRequest
{
    public long? ArticleId { get; init; }

    public string? Scope { get; init; }

    public bool? IsActive { get; init; }
    public bool? IsIndexable { get; init; }

    public string? Keyword { get; init; }

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    public string SortBy { get; init; } = "UpdatedAt";
    public string SortDirection { get; init; } = "DESC";
}