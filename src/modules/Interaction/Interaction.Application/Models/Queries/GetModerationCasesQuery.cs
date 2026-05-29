namespace Interaction.Application.Models.Queries;

public sealed record GetModerationCasesQuery(
    string? Status,
    string? Priority,
    string? ArticlePublicId,
    string? CommentPublicId,
    bool? AlertTriggered,
    int Page,
    int PageSize);