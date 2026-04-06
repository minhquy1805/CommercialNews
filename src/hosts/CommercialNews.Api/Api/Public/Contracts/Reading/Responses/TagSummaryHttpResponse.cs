namespace CommercialNews.Api.Api.Public.Contracts.Reading.Responses;

public sealed class TagSummaryHttpResponse
{
    public long TagId { get; init; }

    public string Name { get; init; } = string.Empty;
}