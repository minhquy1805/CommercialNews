using Audit.Domain.Policies.Redaction;
using Audit.Domain.ValueObjects.Evidence;

namespace Audit.Application.Services.Redaction;

public sealed class AuditRedactionService : IAuditRedactionService
{
    private readonly IAuditRedactionPolicy _redactionPolicy;

    public AuditRedactionService(
        IAuditRedactionPolicy redactionPolicy)
    {
        _redactionPolicy = redactionPolicy
            ?? throw new ArgumentNullException(nameof(redactionPolicy));
    }

    public AuditRedactionOutput Redact(
        AuditRedactionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var payloadResult = _redactionPolicy.Redact(
            input.SourceModule,
            input.EventType,
            input.PayloadJson);

        if (!payloadResult.IsAllowed)
        {
            return ToBlockedOutput(payloadResult);
        }

        var headersResult = _redactionPolicy.RedactHeaders(
            input.SourceModule,
            input.EventType,
            input.HeadersJson);

        if (!headersResult.IsAllowed)
        {
            return ToBlockedOutput(headersResult);
        }

        var metadataResult = _redactionPolicy.RedactMetadata(
            input.SourceModule,
            input.EventType,
            input.MetadataJson);

        if (!metadataResult.IsAllowed)
        {
            return ToBlockedOutput(metadataResult);
        }

        var beforeResult = _redactionPolicy.Redact(
            input.SourceModule,
            input.EventType,
            input.BeforeJson);

        if (!beforeResult.IsAllowed)
        {
            return ToBlockedOutput(beforeResult);
        }

        var afterResult = _redactionPolicy.Redact(
            input.SourceModule,
            input.EventType,
            input.AfterJson);

        if (!afterResult.IsAllowed)
        {
            return ToBlockedOutput(afterResult);
        }

        var changesResult = _redactionPolicy.Redact(
            input.SourceModule,
            input.EventType,
            input.ChangesJson);

        if (!changesResult.IsAllowed)
        {
            return ToBlockedOutput(changesResult);
        }

        var jsonPayload = AuditJsonPayload.Create(
            metadataJson: metadataResult.SanitizedJson,
            headersJson: headersResult.SanitizedJson,
            sanitizedPayloadJson: payloadResult.SanitizedJson,
            beforeJson: beforeResult.SanitizedJson,
            afterJson: afterResult.SanitizedJson,
            changesJson: changesResult.SanitizedJson);

        return AuditRedactionOutput.Allowed(
            jsonPayload: jsonPayload,
            redactedFields: MergeRedactedFields(
                payloadResult,
                headersResult,
                metadataResult,
                beforeResult,
                afterResult,
                changesResult),
            policyName: ResolvePolicyName(
                payloadResult,
                headersResult,
                metadataResult,
                beforeResult,
                afterResult,
                changesResult),
            redactionVersion: ResolveRedactionVersion(
                payloadResult,
                headersResult,
                metadataResult,
                beforeResult,
                afterResult,
                changesResult));
    }

    private static AuditRedactionOutput ToBlockedOutput(
        AuditRedactionResult result)
    {
        return AuditRedactionOutput.Blocked(
            reason: result.Reason ?? "Audit redaction policy blocked the payload.",
            blockedFields: result.BlockedFields.ToArray(),
            policyName: result.PolicyName,
            redactionVersion: result.RedactionVersion);
    }

    private static IReadOnlyList<string> MergeRedactedFields(
        params AuditRedactionResult[] results)
    {
        return results
            .SelectMany(result => result.RedactedFields)
            .Where(field => !string.IsNullOrWhiteSpace(field))
            .Select(field => field.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string? ResolvePolicyName(
        params AuditRedactionResult[] results)
    {
        return results
            .Select(result => result.PolicyName)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
            ?.Trim();
    }

    private static string? ResolveRedactionVersion(
        params AuditRedactionResult[] results)
    {
        return results
            .Select(result => result.RedactionVersion)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
            ?.Trim();
    }
}