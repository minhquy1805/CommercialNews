namespace Reading.Application.Contracts.Articles.Requests;

public sealed class GetArticleByPublicIdRequest
{
    public string ArticlePublicId { get; set; } = string.Empty;
}