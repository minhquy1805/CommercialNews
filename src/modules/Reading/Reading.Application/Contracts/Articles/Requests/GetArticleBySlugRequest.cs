namespace Reading.Application.Contracts.Articles.Requests;

public sealed class GetArticleBySlugRequest
{
    public string Slug { get; set; } = string.Empty;
}