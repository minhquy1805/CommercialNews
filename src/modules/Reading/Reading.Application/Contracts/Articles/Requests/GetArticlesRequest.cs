using Reading.Domain.Constants;

namespace Reading.Application.Contracts.Articles.Requests;

public sealed class GetArticlesRequest
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public long? CategoryId { get; set; }

    public long? TagId { get; set; }

    public string? Keyword { get; set; }

    public string Sort { get; set; } = ReadingSortValues.Default;
}