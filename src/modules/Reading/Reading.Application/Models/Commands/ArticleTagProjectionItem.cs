namespace Reading.Application.Models.Commands;

public sealed record ArticleTagProjectionItem(
    long TagId,
    string? TagPublicId,
    string Name,
    string? Slug);
