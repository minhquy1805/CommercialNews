namespace CommercialNews.Api.Api.Public.Contracts.Reading.Responses;

public sealed class SeoSummaryHttpResponse
{
    public string? CanonicalUrl { get; init; }

    public string? MetaTitle { get; init; }

    public string? MetaDescription { get; init; }
}