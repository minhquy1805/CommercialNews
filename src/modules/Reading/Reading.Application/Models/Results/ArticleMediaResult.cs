namespace Reading.Application.Models.Results;

public sealed class ArticleMediaResult
{
    public long MediaId { get; init; }

    public string Url { get; init; } = string.Empty;

    public string? Alt { get; init; }

    public string MediaType { get; init; } = string.Empty;

    public bool IsPrimary { get; init; }

    public int SortOrder { get; init; }
}