using Reading.Domain.Constants;

namespace Reading.Application.Models.Queries;

public sealed record GetArticlesQuery(
    int Page = 1,
    int PageSize = 20,
    long? CategoryId = null,
    long? TagId = null,
    string? Keyword = null,
    string Sort = ReadingSortValues.Default);