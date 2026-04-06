using Reading.Domain.Enums;

namespace Reading.Application.Contracts.Requests;

public sealed class SearchArticlesRequest
{
    public string Q { get; set; } = string.Empty;

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string? Sort { get; set; } = ReadingSortValues.PublishedAtDescending;
}