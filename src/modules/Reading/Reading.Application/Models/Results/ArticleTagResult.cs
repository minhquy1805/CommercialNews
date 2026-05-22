namespace Reading.Application.Models.Results;

public sealed class ArticleTagResult
{
    public long TagId { get; init; }

    public string? TagPublicId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Slug { get; init; }
}