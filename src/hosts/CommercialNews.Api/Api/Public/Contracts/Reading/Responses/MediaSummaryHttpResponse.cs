namespace CommercialNews.Api.Api.Public.Contracts.Reading.Responses;

public sealed class MediaSummaryHttpResponse
{
    public long MediaId { get; init; }

    public string Url { get; init; } = string.Empty;

    public string? Alt { get; init; }

    public bool IsPrimary { get; init; }

    public int Order { get; init; }
}