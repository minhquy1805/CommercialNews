namespace Media.Application.Contracts.ArticleMedia.Responses;

public sealed class AttachMediaToArticleResponse
{
    public long? ArticleMediaId { get; init; }

    public long ArticleId { get; init; }
    public long MediaId { get; init; }

    public bool Attached { get; init; }
    public bool IsPrimary { get; init; }

    public int AffectedRows { get; init; }
}