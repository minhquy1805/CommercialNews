using Reading.Domain.Constants;

namespace Reading.Application.Contracts.Articles.Requests;

public sealed class SearchArticlesRequest
{
    public string Query { get; set; } = string.Empty;

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string Sort { get; set; } = ReadingSortValues.PublishedAtDescending;
}