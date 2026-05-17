namespace CommercialNews.Api.Api.Admin.Contracts.Seo.SlugRoutes.Responses;

public sealed class CheckSlugAvailabilityHttpResponse
{
    public string Scope { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public bool IsAvailable { get; init; }

    public bool BelongsToCurrentResource { get; init; }

    public string? ExistingResourceType { get; init; }

    public string? ExistingResourcePublicId { get; init; }

    public long? ExistingSlugId { get; init; }
}
