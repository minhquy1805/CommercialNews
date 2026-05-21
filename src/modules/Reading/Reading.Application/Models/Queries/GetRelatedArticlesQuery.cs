namespace Reading.Application.Models.Queries;

public sealed record GetRelatedArticlesQuery(
    string ArticlePublicId,
    int Limit = 6);