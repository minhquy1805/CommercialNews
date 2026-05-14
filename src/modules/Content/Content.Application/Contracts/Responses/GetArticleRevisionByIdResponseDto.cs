namespace Content.Application.Contracts.Responses;

public sealed class GetArticleRevisionByIdResponseDto
{
    public long RevisionId { get; init; }

    public long ArticleId { get; init; }

    public DateTime EditedAt { get; init; }

    public long EditedByUserId { get; init; }

    public long? ArticleVersion { get; init; }

    public string? CorrelationId { get; init; }

    public string? ChangeSummary { get; init; }

    public string? OldTitle { get; init; }

    public string? OldSummary { get; init; }

    public string? OldBody { get; init; }
}