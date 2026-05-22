namespace Reading.Application.Models.Commands;

public sealed record ReorderArticleMediaProjectionCommand(
    long ArticleId,
    IReadOnlyCollection<ArticleMediaProjectionOrderItem> Items,
    long SourceVersion,
    string? MessageId,
    DateTime? SourceOccurredAtUtc);

public sealed record ArticleMediaProjectionOrderItem(
    long MediaId,
    int SortOrder);
