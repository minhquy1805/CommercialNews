namespace Content.Application.Contracts.Responses;

public sealed class ArchiveArticleResponseDto
{
    public long ArticleId { get; init; }

    public string ArticlePublicId { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTime? ArchivedAt { get; init; }

    public long Version { get; init; }

    public DateTime UpdatedAt { get; init; }
}