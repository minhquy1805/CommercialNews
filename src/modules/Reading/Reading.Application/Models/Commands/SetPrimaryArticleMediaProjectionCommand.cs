namespace Reading.Application.Models.Commands;

public sealed record SetPrimaryArticleMediaProjectionCommand(
    long ArticleId,
    long MediaId,
    string MediaPublicId,
    string Url,
    string? Alt,
    string? Caption,
    string MediaType,
    int SortOrder,
    long SourceVersion,
    string? MessageId,
    DateTime? SourceOccurredAtUtc);
