namespace Seo.Application.Models.QueryModels;

public sealed class ResolveSeoRouteResult
{
    public string Scope { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string ResourceType { get; init; } = string.Empty;
    public long ResourceId { get; init; }

    public string? CanonicalUrl { get; init; }

    public bool IsIndexable { get; init; }

    public string Status { get; init; } = string.Empty;

    public int Version { get; init; }
}