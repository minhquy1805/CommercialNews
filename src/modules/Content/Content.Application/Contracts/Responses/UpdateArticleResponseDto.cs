namespace Content.Application.Contracts.Responses;

public sealed class UpdateArticleResponseDto
{
    public long ArticleId { get; init; }

    public string ArticlePublicId { get; init; } = string.Empty;

    public long CategoryId { get; init; }

    public long AuthorUserId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public string Body { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public long? CoverMediaId { get; init; }

    public IReadOnlyCollection<long> TagIds { get; init; } = Array.Empty<long>();

    public long Version { get; init; }

    public DateTime UpdatedAt { get; init; }
}