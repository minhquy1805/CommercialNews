namespace Media.Application.Models.Commands;

public sealed record AttachArticleMediaCommand(
    long ArticleId,
    long MediaId,
    bool IsPrimary,
    long? CreatedBy);