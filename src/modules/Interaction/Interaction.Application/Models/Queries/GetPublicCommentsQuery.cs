namespace Interaction.Application.Models.Queries;

public sealed record GetPublicCommentsQuery(
    string ArticlePublicId,
    int Page,
    int PageSize,
    string SortDirection);