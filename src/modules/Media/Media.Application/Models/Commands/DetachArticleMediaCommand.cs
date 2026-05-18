namespace Media.Application.Models.Commands;

public sealed record DetachArticleMediaCommand(
    long ArticleId,
    long MediaId,
    long? DeletedBy);