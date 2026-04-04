namespace Media.Application.Contracts.ArticleMedia.Requests;

public sealed class AttachMediaToArticleRequest
{
    public long ArticleId { get; init; }

    public long MediaId { get; init; }

    public bool IsPrimary { get; init; }

    public long? ActorUserId { get; init; }
}