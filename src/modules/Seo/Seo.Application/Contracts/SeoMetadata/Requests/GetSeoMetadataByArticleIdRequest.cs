namespace Seo.Application.Contracts.SeoMetadata.Requests;

public sealed class GetSeoMetadataByArticleIdRequest
{
    public long ArticleId { get; init; }
}