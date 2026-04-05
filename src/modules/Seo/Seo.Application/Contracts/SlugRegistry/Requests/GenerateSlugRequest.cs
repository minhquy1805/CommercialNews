namespace Seo.Application.Contracts.SlugRegistry.Requests;

public sealed class GenerateSlugRequest
{
    public string Source { get; init; } = string.Empty;

    public string Scope { get; init; } = string.Empty;
}