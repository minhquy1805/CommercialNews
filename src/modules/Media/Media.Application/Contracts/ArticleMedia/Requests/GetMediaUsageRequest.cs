namespace Media.Application.Contracts.ArticleMedia.Requests;

public sealed class GetMediaUsageRequest
{
    public long MediaId { get; init; }

    public bool IncludeDeleted { get; init; }
}