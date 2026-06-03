using Audit.Domain.ValueObjects.Common;

namespace Audit.Application.Services.Normalization;

public sealed record AuditEventNormalizationContext(
    AuditSourceEvent SourceEvent,
    AuditAggregateRef AggregateRef,
    AuditTraceContext TraceContext,
    string PayloadJson,
    string? HeadersJson,
    long? InitiatorUserId);