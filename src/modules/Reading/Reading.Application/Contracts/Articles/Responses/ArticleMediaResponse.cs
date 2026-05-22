namespace Reading.Application.Contracts.Articles.Responses;

public sealed class ArticleMediaResponse
{
    public long MediaId { get; set; }

    public string MediaPublicId { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string? Alt { get; set; }

    public string? Caption { get; set; }

    public string MediaType { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }

    public int SortOrder { get; set; }
}
