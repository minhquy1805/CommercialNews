namespace Reading.Application.Models.Commands;

public sealed record MarkArticleProjectionNotPublicCommand(
    long ArticleId,
    string Status,
    long SourceVersion,
    string? MessageId,
    DateTime? SourceOccurredAtUtc);