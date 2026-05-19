namespace Media.Application.Contracts.ArticleMedia.Requests;

public sealed class DetachMediaFromArticleRequest
{
    public long ArticleId { get; init; }

    public long MediaId { get; init; }
}