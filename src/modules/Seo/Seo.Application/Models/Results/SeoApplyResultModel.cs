namespace Seo.Application.Models.Results;

public sealed class SeoApplyResultModel
{
    public string ApplyResult { get; init; } = string.Empty;
    public long? EntityId { get; init; }
    public int? Version { get; init; }
    public long? SourceAggregateVersion { get; init; }
    public string? LastAppliedMessageId { get; init; }
    public DateTime? LastSyncedAtUtc { get; init; }
}