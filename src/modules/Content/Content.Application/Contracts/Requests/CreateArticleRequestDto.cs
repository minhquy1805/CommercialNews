namespace Content.Application.Contracts.Requests;

public sealed class CreateArticleRequestDto
{
    public long CategoryId { get; init; }

    public long AuthorUserId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public string Body { get; init; } = string.Empty;

    public long? CoverMediaId { get; init; }

    public IReadOnlyCollection<long> TagIds { get; init; } = Array.Empty<long>();

    public long? ActorUserId { get; init; }
}