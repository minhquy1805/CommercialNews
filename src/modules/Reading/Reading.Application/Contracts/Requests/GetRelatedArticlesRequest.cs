namespace Reading.Application.Contracts.Requests;

public sealed class GetRelatedArticlesRequest
{
    public long ArticleId { get; set; }

    public int Limit { get; set; } = 6;
}