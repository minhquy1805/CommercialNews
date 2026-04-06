using Reading.Domain.Enums;

namespace Reading.Application.Models.QueryModels;

public sealed class ReadingSearchArticlesQuery
{
    public string Q { get; init; } = string.Empty;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string Sort { get; init; } = ReadingSortValues.PublishedAtDescending;
}