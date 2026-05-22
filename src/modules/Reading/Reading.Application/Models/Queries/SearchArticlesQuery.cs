using Reading.Domain.Constants;

namespace Reading.Application.Models.Queries;

public sealed record SearchArticlesQuery(
    string Query,
    int Page = 1,
    int PageSize = 20,
    string Sort = ReadingSortValues.Default);