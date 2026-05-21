namespace Reading.Application.Models.Queries;

public sealed record GetArticleByPublicIdQuery(
    string ArticlePublicId);