namespace Seo.Application.Contracts.EventApply;

public sealed class SeoEventApplyResult
{
    public string MessageId { get; init; } = string.Empty;

    public string EventType { get; init; } = string.Empty;

    public string ResourceType { get; init; } = string.Empty;

    public string ResourcePublicId { get; init; } = string.Empty;

    public SeoApplyOperationResult? SlugRoute { get; init; }

    public SeoApplyOperationResult? Metadata { get; init; }

    public bool WasApplied =>
        SlugRoute?.WasApplied == true ||
        Metadata?.WasApplied == true;

    public bool WasDeduped =>
        SlugRoute?.WasDeduped == true ||
        Metadata?.WasDeduped == true;

    public bool WasStaleIgnored =>
        SlugRoute?.WasStaleIgnored == true ||
        Metadata?.WasStaleIgnored == true;

    public static SeoEventApplyResult From(
        string messageId,
        string eventType,
        string resourceType,
        string resourcePublicId,
        SeoApplyOperationResult? slugRoute,
        SeoApplyOperationResult? metadata)
    {
        return new SeoEventApplyResult
        {
            MessageId = messageId,
            EventType = eventType,
            ResourceType = resourceType,
            ResourcePublicId = resourcePublicId,
            SlugRoute = slugRoute,
            Metadata = metadata
        };
    }
}