namespace Media.Application.Models.Commands;

public sealed record RestoreArticleMediaCommand(
    long ArticleId,
    long MediaId,
    long? RestoredBy);