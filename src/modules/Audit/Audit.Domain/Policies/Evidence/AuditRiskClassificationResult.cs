using Audit.Domain.ValueObjects.Evidence;

namespace Audit.Domain.Policies.Evidence;

public sealed record AuditRiskClassificationResult
{
    public AuditRisk Risk { get; }
    public string? MatchedRule { get; }
    public bool RequiresReview { get; }

    private AuditRiskClassificationResult(
        AuditRisk risk,
        string? matchedRule,
        bool requiresReview)
    {
        Risk = risk;
        MatchedRule = matchedRule;
        RequiresReview = requiresReview;
    }

    public static AuditRiskClassificationResult Create(
        AuditRisk risk,
        string? matchedRule = null,
        bool requiresReview = false)
    {
        ArgumentNullException.ThrowIfNull(risk);

        return new AuditRiskClassificationResult(
            risk,
            NormalizeOptional(matchedRule),
            requiresReview);
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized;
    }
}