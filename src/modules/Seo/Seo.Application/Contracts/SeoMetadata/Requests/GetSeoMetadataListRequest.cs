namespace Seo.Application.Contracts.SeoMetadata.Requests;

public sealed class GetSeoMetadataListRequest
{
    public long? ArticleId { get; init; }

    public long? UpdatedByUserId { get; init; }

    public string? Keyword { get; init; }

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    public string SortBy { get; init; } = "UpdatedAt";
    public string SortDirection { get; init; } = "DESC";
}