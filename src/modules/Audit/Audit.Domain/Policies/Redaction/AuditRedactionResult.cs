using System.Collections.Immutable;

namespace Audit.Domain.Policies.Redaction;

public sealed record AuditRedactionResult
{
    public bool IsAllowed { get; }
    public string? SanitizedJson { get; }
    public string? PolicyName { get; }
    public string? RedactionVersion { get; }
    public ImmutableArray<string> RedactedFields { get; }
    public ImmutableArray<string> BlockedFields { get; }
    public string? Reason { get; }

    private AuditRedactionResult(
        bool isAllowed,
        string? sanitizedJson,
        string? policyName,
        string? redactionVersion,
        ImmutableArray<string> redactedFields,
        ImmutableArray<string> blockedFields,
        string? reason)
    {
        IsAllowed = isAllowed;
        SanitizedJson = sanitizedJson;
        PolicyName = policyName;
        RedactionVersion = redactionVersion;
        RedactedFields = redactedFields;
        BlockedFields = blockedFields;
        Reason = reason;
    }

    public static AuditRedactionResult Allowed(
        string? sanitizedJson,
        string? policyName = null,
        string? redactionVersion = null,
        IEnumerable<string>? redactedFields = null)
    {
        return new AuditRedactionResult(
            isAllowed: true,
            sanitizedJson: NormalizeOptional(sanitizedJson),
            policyName: NormalizeOptional(policyName),
            redactionVersion: NormalizeOptional(redactionVersion),
            redactedFields: ToImmutableCleanArray(redactedFields),
            blockedFields: ImmutableArray<string>.Empty,
            reason: null);
    }

    public static AuditRedactionResult Blocked(
        string reason,
        IEnumerable<string>? blockedFields = null,
        string? policyName = null,
        string? redactionVersion = null)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Redaction block reason is required.", nameof(reason));
        }

        return new AuditRedactionResult(
            isAllowed: false,
            sanitizedJson: null,
            policyName: NormalizeOptional(policyName),
            redactionVersion: NormalizeOptional(redactionVersion),
            redactedFields: ImmutableArray<string>.Empty,
            blockedFields: ToImmutableCleanArray(blockedFields),
            reason: reason.Trim());
    }

    private static ImmutableArray<string> ToImmutableCleanArray(IEnumerable<string>? values)
    {
        if (values is null)
        {
            return ImmutableArray<string>.Empty;
        }

        return values
            .Select(NormalizeOptional)
            .Where(value => value is not null)
            .Select(value => value!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToImmutableArray();
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized;
    }
}