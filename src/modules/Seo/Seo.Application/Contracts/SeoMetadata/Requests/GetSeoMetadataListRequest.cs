namespace Seo.Application.Contracts.SeoMetadata.Requests;

public sealed class GetSeoMetadataListRequest
{
    public string? Scope { get; init; }

    public string? ResourceType { get; init; }

    public string? ResourcePublicId { get; init; }

    public bool? IsManualOverride { get; init; }

    public long? UpdatedByUserId { get; init; }

    public string? Keyword { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string SortBy { get; init; } = "UpdatedAtUtc";

    public string SortDirection { get; init; } = "DESC";
}