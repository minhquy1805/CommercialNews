namespace CommercialNews.Api.Api.Public.Contracts.Reading.Responses;

public sealed class CategorySummaryHttpResponse
{
    public long CategoryId { get; init; }

    public string Name { get; init; } = string.Empty;
}