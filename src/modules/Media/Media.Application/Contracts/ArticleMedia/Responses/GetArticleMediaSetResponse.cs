namespace Media.Application.Contracts.ArticleMedia.Responses;

public sealed class GetArticleMediaSetResponse
{
    public long ArticleId { get; init; }

    public int Version { get; init; }

    public DateTime CreatedAt { get; init; }
    public long? CreatedBy { get; init; }

    public DateTime UpdatedAt { get; init; }
    public long? UpdatedBy { get; init; }
}