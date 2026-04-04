namespace Media.Application.Contracts.ArticleMedia.Responses;

public sealed class SetPrimaryMediaResponse
{
    public long ArticleId { get; init; }

    public long MediaId { get; init; }

    public bool PrimarySet { get; init; }

    public int AffectedRows { get; init; }
}