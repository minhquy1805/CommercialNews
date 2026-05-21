namespace Reading.Application.Models.Commands;

public sealed record ApplyContentArticleProjectionCommand(
    long ArticleId,
    string ArticlePublicId,
    string Title,
    string Summary,
    string Body,
    long? CategoryId,
    string? CategoryName,
    long? AuthorUserId,
    string? AuthorDisplayName,
    string Status,
    bool IsPublic,
    DateTime? PublishedAtUtc,
    DateTime UpdatedAtUtc,
    long SourceVersion,
    string? MessageId,
    DateTime? SourceOccurredAtUtc);