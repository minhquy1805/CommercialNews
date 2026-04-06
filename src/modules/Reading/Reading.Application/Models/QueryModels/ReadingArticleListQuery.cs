using Reading.Domain.Enums;

namespace Reading.Application.Models.QueryModels;

public sealed class ReadingArticleListQuery
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public long? CategoryId { get; init; }

    public long? TagId { get; init; }

    public string? Q { get; init; }

    public string Sort { get; init; } = ReadingSortValues.PublishedAtDescending;
}