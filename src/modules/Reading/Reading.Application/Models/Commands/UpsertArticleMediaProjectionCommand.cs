namespace Reading.Application.Models.Commands;

public sealed record UpsertArticleMediaProjectionCommand(
    long ArticleId,
    long MediaId,
    string MediaPublicId,
    string Url,
    string? Alt,
    string? Caption,
    string MediaType,
    int SortOrder,
    bool IsPrimary,
    long SourceVersion,
    string? MessageId,
    DateTime? SourceOccurredAtUtc);
