namespace Media.Application.Contracts.ArticleMedia.Responses;

public sealed class RestoreArticleMediaResponse
{
    public long ArticleId { get; init; }
    public long MediaId { get; init; }

    public bool Restored { get; init; }

    public int AffectedRows { get; init; }
}