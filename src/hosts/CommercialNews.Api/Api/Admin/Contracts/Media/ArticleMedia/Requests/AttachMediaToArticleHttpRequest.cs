namespace CommercialNews.Api.Api.Admin.Contracts.Media.ArticleMedia.Requests;

public sealed class AttachMediaToArticleHttpRequest
{
    public long MediaId { get; init; }
    public bool IsPrimary { get; init; }
}