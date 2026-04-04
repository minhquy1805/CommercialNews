namespace Media.Application.Contracts.ArticleMedia.Requests;

public sealed class RestoreArticleMediaRequest
{
    public long ArticleId { get; init; }

    public long MediaId { get; init; }

    public long? ActorUserId { get; init; }
}