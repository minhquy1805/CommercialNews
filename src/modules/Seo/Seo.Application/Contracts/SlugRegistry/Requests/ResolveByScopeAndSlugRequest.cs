namespace Seo.Application.Contracts.SlugRegistry.Requests;

public sealed class ResolveByScopeAndSlugRequest
{
    public string Scope { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;
}