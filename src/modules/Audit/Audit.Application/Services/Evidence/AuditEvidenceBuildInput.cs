using Audit.Application.Services.Normalization;
using Audit.Domain.ValueObjects.Common;
using Audit.Domain.ValueObjects.Evidence;

namespace Audit.Application.Services.Evidence;

public sealed record AuditEvidenceBuildInput(
    string PublicId,
    AuditSourceEvent SourceEvent,
    AuditAggregateRef AggregateRef,
    AuditTraceContext TraceContext,
    AuditNormalizedEvent NormalizedEvent,
    AuditJsonPayload JsonPayload,
    DateTime IngestedAtUtc);