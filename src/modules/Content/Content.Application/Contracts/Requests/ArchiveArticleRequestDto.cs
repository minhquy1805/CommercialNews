namespace Content.Application.Contracts.Requests;

public sealed class ArchiveArticleRequestDto
{
    public long ArticleId { get; init; }

    public long ExpectedVersion { get; init; }

    public long? ActorUserId { get; init; }
}