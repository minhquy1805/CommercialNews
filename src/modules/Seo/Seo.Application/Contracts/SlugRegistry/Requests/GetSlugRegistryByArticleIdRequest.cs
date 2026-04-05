namespace Seo.Application.Contracts.SlugRegistry.Requests;

public sealed class GetSlugRegistryByArticleIdRequest
{
    public long ArticleId { get; init; }

    public string? Scope { get; init; }

    public bool? OnlyActive { get; init; }
}