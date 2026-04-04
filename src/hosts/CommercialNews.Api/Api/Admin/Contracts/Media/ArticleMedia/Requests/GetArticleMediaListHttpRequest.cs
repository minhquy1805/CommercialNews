namespace CommercialNews.Api.Api.Admin.Contracts.Media.ArticleMedia.Requests;

public sealed class GetArticleMediaListHttpRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public bool IncludeDeleted { get; init; }
    public string SortBy { get; init; } = "SortOrder";
    public string SortDirection { get; init; } = "ASC";
}