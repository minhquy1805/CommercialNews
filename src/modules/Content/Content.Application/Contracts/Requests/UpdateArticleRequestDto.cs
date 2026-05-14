namespace Content.Application.Contracts.Requests;

public sealed class UpdateArticleRequestDto
{
    public long ArticleId { get; init; }

    public long ExpectedVersion { get; init; }

    public long CategoryId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public string Body { get; init; } = string.Empty;

    public long? CoverMediaId { get; init; }

    public IReadOnlyCollection<long> TagIds { get; init; } = Array.Empty<long>();

    public string? ChangeSummary { get; init; }

    public long? ActorUserId { get; init; }
}