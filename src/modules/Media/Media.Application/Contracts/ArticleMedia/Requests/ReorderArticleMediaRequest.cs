namespace Media.Application.Contracts.ArticleMedia.Requests;

public sealed class ReorderArticleMediaRequest
{
    public long ArticleId { get; init; }

    public IReadOnlyCollection<ReorderArticleMediaItemRequest> Items { get; init; }
        = Array.Empty<ReorderArticleMediaItemRequest>();

    public long? ActorUserId { get; init; }
}

public sealed class ReorderArticleMediaItemRequest
{
    public long MediaId { get; init; }

    public int SortOrder { get; init; }
}