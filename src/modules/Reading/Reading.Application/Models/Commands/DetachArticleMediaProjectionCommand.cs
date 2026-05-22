namespace Reading.Application.Models.Commands;

public sealed record DetachArticleMediaProjectionCommand(
    long ArticleId,
    long MediaId,
    bool PrimaryCleared,
    long SourceVersion,
    string? MessageId,
    DateTime? SourceOccurredAtUtc);
