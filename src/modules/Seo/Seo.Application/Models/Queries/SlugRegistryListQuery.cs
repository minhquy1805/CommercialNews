namespace Seo.Application.Models.Queries;

public sealed class SlugRegistryListQuery
{
    public int Skip { get; init; }

    public int Take { get; init; } = 20;

    public string? Scope { get; init; }

    public string? ResourceType { get; init; }

    public string? ResourcePublicId { get; init; }

    public bool? IsActive { get; init; }

    public bool? IsIndexable { get; init; }

    public string? Keyword { get; init; }

    public string? SortBy { get; init; } = "UpdatedAtUtc";

    public string? SortDirection { get; init; } = "DESC";
}