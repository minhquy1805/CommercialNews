namespace Content.Application.Contracts.Responses
{
    public sealed class ArticleRevisionListItemDto
    {
        public long RevisionId { get; init; }

        public int RevisionNumber { get; init; }

        public string TitleSnapshot { get; init; } = string.Empty;
        public string? SummarySnapshot { get; init; }
        public string BodySnapshot { get; init; } = string.Empty;

        public long? CategoryIdSnapshot { get; init; }
        public string StatusSnapshot { get; init; } = string.Empty;
        public long? CoverMediaIdSnapshot { get; init; }

        public DateTime ChangedAt { get; init; }
        public long? ChangedByUserId { get; init; }

        public string ChangeType { get; init; } = string.Empty;
        public string? ChangeSummary { get; init; }
    }
}