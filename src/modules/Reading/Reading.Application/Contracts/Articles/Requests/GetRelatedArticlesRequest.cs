namespace Reading.Application.Contracts.Articles.Requests;

public sealed class GetRelatedArticlesRequest
{
    public string ArticlePublicId { get; set; } = string.Empty;

    public int Limit { get; set; } = 6;
}