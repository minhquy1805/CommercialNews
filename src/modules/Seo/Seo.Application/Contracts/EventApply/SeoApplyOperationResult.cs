namespace Seo.Application.Contracts.EventApply;

public sealed class SeoApplyOperationResult
{
    public string Operation { get; init; } = string.Empty;

    public string ApplyResult { get; init; } = string.Empty;

    public long? EntityId { get; init; }

    public int? Version { get; init; }

    public long? SourceAggregateVersion { get; init; }

    public string? LastAppliedMessageId { get; init; }

    public DateTime? LastSyncedAtUtc { get; init; }

    public bool WasApplied =>
        string.Equals(ApplyResult, "Applied", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(ApplyResult, "Inserted", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(ApplyResult, "Updated", StringComparison.OrdinalIgnoreCase);

    public bool WasDeduped =>
        string.Equals(ApplyResult, "Deduped", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(ApplyResult, "DuplicateIgnored", StringComparison.OrdinalIgnoreCase);

    public bool WasStaleIgnored =>
        string.Equals(ApplyResult, "StaleIgnored", StringComparison.OrdinalIgnoreCase);

    public static SeoApplyOperationResult From(
        string operation,
        Models.Results.SeoApplyResultModel result)
    {
        return new SeoApplyOperationResult
        {
            Operation = operation,
            ApplyResult = result.ApplyResult,
            EntityId = result.EntityId,
            Version = result.Version,
            SourceAggregateVersion = result.SourceAggregateVersion,
            LastAppliedMessageId = result.LastAppliedMessageId,
            LastSyncedAtUtc = result.LastSyncedAtUtc
        };
    }
}