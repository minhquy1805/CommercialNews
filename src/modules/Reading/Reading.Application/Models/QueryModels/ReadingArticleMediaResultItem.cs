namespace Reading.Application.Models.QueryModels;

public sealed class ReadingArticleMediaResultItem
{
    public long MediaId { get; init; }

    public string Url { get; init; } = string.Empty;

    public string? Alt { get; init; }

    public bool IsPrimary { get; init; }

    public int Order { get; init; }
}