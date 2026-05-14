namespace Content.Application.Contracts.Requests;

public sealed class UnpublishArticleRequestDto
{
    public long ArticleId { get; init; }

    public long ExpectedVersion { get; init; }

    public string Reason { get; init; } = string.Empty;

    public long? ActorUserId { get; init; }
}