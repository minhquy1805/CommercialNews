namespace Media.Application.Contracts.ArticleMedia.Requests;

public sealed class GetArticlePrimaryMediaRequest
{
    public long ArticleId { get; init; }
}