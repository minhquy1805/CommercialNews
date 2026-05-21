namespace Reading.Application.Contracts.Articles.Responses;

public sealed class ArticleTagResponse
{
    public long TagId { get; set; }

    public string? TagPublicId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Slug { get; set; }
}