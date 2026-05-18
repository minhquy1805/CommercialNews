namespace Media.Application.Models.Commands;

public sealed record ReorderArticleMediaCommand(
    long ArticleId,
    int? ExpectedVersion,
    IReadOnlyList<ArticleMediaOrderItem> Orders,
    long? UpdatedBy);