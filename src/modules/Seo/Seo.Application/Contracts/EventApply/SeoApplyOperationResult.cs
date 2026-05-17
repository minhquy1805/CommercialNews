using Seo.Domain.Constants;

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
        IsApplyResult(SeoApplyResults.Applied);

    public bool WasDeduped => false;

    public bool WasStaleIgnored =>
        IsApplyResult(SeoApplyResults.StaleIgnored);

    public bool WasNoRouteToActivate =>
        IsApplyResult(SeoApplyResults.NoRouteToActivate);

    public bool WasNoRouteToDeactivate =>
        IsApplyResult(SeoApplyResults.NoRouteToDeactivate);

    public bool WasNotApplied =>
        IsApplyResult(SeoApplyResults.NotApplied);

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

    private bool IsApplyResult(string expected)
    {
        return string.Equals(
            ApplyResult,
            expected,
            StringComparison.OrdinalIgnoreCase);
    }
}
