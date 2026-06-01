using Audit.Domain.Exceptions;
using System.Text.Json;

namespace Audit.Domain.ValueObjects.Evidence;

public sealed record AuditJsonPayload
{
    public string? MetadataJson { get; }
    public string? HeadersJson { get; }
    public string? SanitizedPayloadJson { get; }
    public string? BeforeJson { get; }
    public string? AfterJson { get; }
    public string? ChangesJson { get; }

    private AuditJsonPayload(
        string? metadataJson,
        string? headersJson,
        string? sanitizedPayloadJson,
        string? beforeJson,
        string? afterJson,
        string? changesJson)
    {
        MetadataJson = metadataJson;
        HeadersJson = headersJson;
        SanitizedPayloadJson = sanitizedPayloadJson;
        BeforeJson = beforeJson;
        AfterJson = afterJson;
        ChangesJson = changesJson;
    }

    public static AuditJsonPayload Create(
        string? metadataJson,
        string? headersJson,
        string? sanitizedPayloadJson,
        string? beforeJson,
        string? afterJson,
        string? changesJson)
    {
        var normalizedMetadataJson = NormalizeOptionalJson(metadataJson);
        var normalizedHeadersJson = NormalizeOptionalJson(headersJson);
        var normalizedSanitizedPayloadJson = NormalizeOptionalJson(sanitizedPayloadJson);
        var normalizedBeforeJson = NormalizeOptionalJson(beforeJson);
        var normalizedAfterJson = NormalizeOptionalJson(afterJson);
        var normalizedChangesJson = NormalizeOptionalJson(changesJson);

        return new AuditJsonPayload(
            normalizedMetadataJson,
            normalizedHeadersJson,
            normalizedSanitizedPayloadJson,
            normalizedBeforeJson,
            normalizedAfterJson,
            normalizedChangesJson);
    }

    public static AuditJsonPayload Empty()
    {
        return new AuditJsonPayload(
            metadataJson: null,
            headersJson: null,
            sanitizedPayloadJson: null,
            beforeJson: null,
            afterJson: null,
            changesJson: null);
    }

    private static string? NormalizeOptionalJson(string? value)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        try
        {
            using var _ = JsonDocument.Parse(normalized);
            return normalized;
        }
        catch (JsonException ex)
        {
            throw AuditDomainException.JsonPayloadInvalid(ex);
        }
    }
}