namespace Reading.Application.Contracts.Requests;

public sealed class GetArticleBySlugRequest
{
    public string Scope { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;
}