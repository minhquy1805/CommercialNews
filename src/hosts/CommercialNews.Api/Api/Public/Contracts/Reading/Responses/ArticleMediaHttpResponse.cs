namespace CommercialNews.Api.Api.Public.Contracts.Reading.Responses;

public sealed class ArticleMediaHttpResponse
{
    public long MediaId { get; init; }

    public string MediaPublicId { get; init; } = string.Empty;

    public string Url { get; init; } = string.Empty;

    public string? Alt { get; init; }

    public string? Caption { get; init; }

    public string MediaType { get; init; } = string.Empty;

    public bool IsPrimary { get; init; }

    public int SortOrder { get; init; }
}
