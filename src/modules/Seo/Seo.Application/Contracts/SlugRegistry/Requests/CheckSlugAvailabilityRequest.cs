namespace Seo.Application.Contracts.SlugRegistry.Requests;

public sealed class CheckSlugAvailabilityRequest
{
    public string? Scope { get; init; }

    public string Slug { get; init; } = string.Empty;

    public string? ResourceType { get; init; }

    public string? ResourcePublicId { get; init; }
}