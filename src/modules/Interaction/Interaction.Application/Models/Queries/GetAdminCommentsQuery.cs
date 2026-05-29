namespace Interaction.Application.Models.Queries;

public sealed record GetAdminCommentsQuery(
    string? Status,
    string? ArticlePublicId,
    long? AuthorUserId,
    int Page,
    int PageSize);