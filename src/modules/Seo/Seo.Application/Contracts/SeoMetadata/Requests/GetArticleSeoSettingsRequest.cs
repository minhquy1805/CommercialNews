namespace Seo.Application.Contracts.SeoMetadata.Requests;

public sealed class GetArticleSeoSettingsRequest
{
    public long ArticleId { get; init; }
}