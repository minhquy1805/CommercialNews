namespace Media.Application.Contracts.ArticleMedia.Responses;

public sealed class ReorderArticleMediaResponse
{
    public long ArticleId { get; init; }

    public bool Reordered { get; init; }

    public int AffectedRows { get; init; }
}