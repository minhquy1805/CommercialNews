namespace CommercialNews.Api.Api.Public.Contracts.Reading.Requests;

public sealed class GetArticleByPublicIdHttpRequest
{
    public string ArticlePublicId { get; init; } = string.Empty;
}