using Audit.Domain.Constants.Common;
using Audit.Domain.Exceptions;

namespace Audit.Domain.ValueObjects.Common;

public sealed record AuditTraceContext
{
    public string? CorrelationId { get; }
    public string? CausationId { get; }
    public string? TraceId { get; }

    private AuditTraceContext(
        string? correlationId,
        string? causationId,
        string? traceId)
    {
        CorrelationId = correlationId;
        CausationId = causationId;
        TraceId = traceId;
    }

    public static AuditTraceContext Create(
        string? correlationId,
        string? causationId,
        string? traceId)
    {
        var normalizedCorrelationId = NormalizeOptional(correlationId);
        if (normalizedCorrelationId is not null &&
            normalizedCorrelationId.Length > AuditConstants.MaxCorrelationIdLength)
        {
            throw AuditDomainException.CorrelationIdTooLong();
        }

        var normalizedCausationId = NormalizeOptional(causationId);
        if (normalizedCausationId is not null &&
            normalizedCausationId.Length > AuditConstants.MaxCausationIdLength)
        {
            throw AuditDomainException.CausationIdTooLong();
        }

        var normalizedTraceId = NormalizeOptional(traceId);
        if (normalizedTraceId is not null &&
            normalizedTraceId.Length > AuditConstants.MaxTraceIdLength)
        {
            throw AuditDomainException.TraceIdTooLong();
        }

        return new AuditTraceContext(
            normalizedCorrelationId,
            normalizedCausationId,
            normalizedTraceId);
    }

    public static AuditTraceContext Empty()
    {
        return new AuditTraceContext(
            correlationId: null,
            causationId: null,
            traceId: null);
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized;
    }
}