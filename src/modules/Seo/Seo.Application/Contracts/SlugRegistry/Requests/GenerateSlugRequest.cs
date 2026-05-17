namespace Seo.Application.Contracts.SlugRegistry.Requests;

public sealed class GenerateSlugRequest
{
    public string Source { get; init; } = string.Empty;

    public string? Scope { get; init; }

    public string? ResourceType { get; init; }

    public string? ResourcePublicId { get; init; }
}