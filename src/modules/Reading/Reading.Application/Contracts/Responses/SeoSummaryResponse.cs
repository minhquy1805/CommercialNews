namespace Reading.Application.Contracts.Responses;

public sealed class SeoSummaryResponse
{
    public string? CanonicalUrl { get; set; }

    public string? MetaTitle { get; set; }

    public string? MetaDescription { get; set; }
}