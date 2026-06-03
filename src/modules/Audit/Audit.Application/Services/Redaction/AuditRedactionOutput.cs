using Audit.Domain.ValueObjects.Evidence;

namespace Audit.Application.Services.Redaction;

public sealed record AuditRedactionOutput(
    bool IsAllowed,
    AuditJsonPayload? JsonPayload,
    string? Reason,
    IReadOnlyList<string> RedactedFields,
    IReadOnlyList<string> BlockedFields,
    string? PolicyName,
    string? RedactionVersion)
{
    public static AuditRedactionOutput Allowed(
        AuditJsonPayload jsonPayload,
        IReadOnlyList<string>? redactedFields = null,
        string? policyName = null,
        string? redactionVersion = null)
    {
        ArgumentNullException.ThrowIfNull(jsonPayload);

        return new AuditRedactionOutput(
            IsAllowed: true,
            JsonPayload: jsonPayload,
            Reason: null,
            RedactedFields: redactedFields ?? Array.Empty<string>(),
            BlockedFields: Array.Empty<string>(),
            PolicyName: NormalizeOptional(policyName),
            RedactionVersion: NormalizeOptional(redactionVersion));
    }

    public static AuditRedactionOutput Blocked(
        string reason,
        IReadOnlyList<string>? blockedFields = null,
        string? policyName = null,
        string? redactionVersion = null)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException(
                "Redaction block reason is required.",
                nameof(reason));
        }

        return new AuditRedactionOutput(
            IsAllowed: false,
            JsonPayload: null,
            Reason: reason.Trim(),
            RedactedFields: Array.Empty<string>(),
            BlockedFields: blockedFields ?? Array.Empty<string>(),
            PolicyName: NormalizeOptional(policyName),
            RedactionVersion: NormalizeOptional(redactionVersion));
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized;
    }
}