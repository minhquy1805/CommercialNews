namespace Media.Application.Models.Commands;

public sealed record SetPrimaryArticleMediaCommand(
    long ArticleId,
    long MediaId,
    int? ExpectedVersion,
    long? UpdatedBy);