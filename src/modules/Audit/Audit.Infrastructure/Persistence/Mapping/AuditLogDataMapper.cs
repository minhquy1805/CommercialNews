using Audit.Domain.Entities;
using Audit.Domain.ValueObjects.Common;
using Audit.Domain.ValueObjects.Evidence;
using Audit.Infrastructure.Persistence.Sql;
using Microsoft.Data.SqlClient;

namespace Audit.Infrastructure.Persistence.Mapping;

internal static class AuditLogDataMapper
{
    public static AuditLog Map(
        SqlDataReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var sourceEvent = AuditSourceEvent.Create(
            messageId: reader.GetRequiredString("MessageId"),
            eventType: reader.GetRequiredString("EventType"),
            eventVersion: reader.GetNullableInt32("EventVersion"),
            sourceModule: reader.GetRequiredString("SourceModule"),
            sourcePriority: reader.GetNullableInt32("SourcePriority"),
            sourceOccurredAtUtc: reader.GetRequiredDateTime("OccurredAtUtc"),
            sourcePublishedAtUtc: null);

        var aggregateRef = AuditAggregateRef.Create(
            aggregateType: reader.GetNullableString("AggregateType"),
            aggregateId: reader.GetNullableString("AggregateId"),
            aggregatePublicId: reader.GetNullableString("AggregatePublicId"),
            aggregateVersion: reader.GetNullableInt32("AggregateVersion"));

        var traceContext = AuditTraceContext.Create(
            correlationId: reader.GetNullableString("CorrelationId"),
            causationId: reader.GetNullableString("CausationId"),
            traceId: reader.GetNullableString("TraceId"));

        var actor = AuditActor.Create(
            actorUserId: reader.GetNullableString("ActorUserId"),
            actorInternalId: reader.GetNullableInt64("ActorInternalId"),
            actorEmail: reader.GetNullableString("ActorEmail"),
            actorDisplayName: reader.GetNullableString("ActorDisplayName"),
            actorType: reader.GetRequiredString("ActorType"));

        var resource = AuditResource.Create(
            resourceType: reader.GetRequiredString("ResourceType"),
            resourceId: reader.GetRequiredString("ResourceId"),
            resourceDisplayName: reader.GetNullableString("ResourceDisplayName"));

        var risk = AuditRisk.Create(
            outcome: reader.GetRequiredString("Outcome"),
            severity: reader.GetRequiredString("Severity"),
            riskLevel: reader.GetRequiredString("RiskLevel"));

        var requestContext = AuditRequestContext.Create(
            ipAddress: reader.GetNullableString("IpAddress"),
            userAgent: reader.GetNullableString("UserAgent"));

        var jsonPayload = AuditJsonPayload.Create(
            metadataJson: reader.GetNullableString("MetadataJson"),
            headersJson: reader.GetNullableString("HeadersJson"),
            sanitizedPayloadJson: reader.GetNullableString("SanitizedPayloadJson"),
            beforeJson: reader.GetNullableString("BeforeJson"),
            afterJson: reader.GetNullableString("AfterJson"),
            changesJson: reader.GetNullableString("ChangesJson"));

        return AuditLog.Rehydrate(
            auditLogId: reader.GetRequiredInt64("AuditLogId"),
            publicId: reader.GetRequiredString("PublicId"),
            sourceEvent: sourceEvent,
            aggregateRef: aggregateRef,
            traceContext: traceContext,
            actor: actor,
            resource: resource,
            risk: risk,
            requestContext: requestContext,
            jsonPayload: jsonPayload,
            action: reader.GetRequiredString("Action"),
            actionCategory: reader.GetNullableString("ActionCategory"),
            summary: reader.GetRequiredString("Summary"),
            reason: null,
            ingestedAtUtc: reader.GetRequiredDateTime("IngestedAtUtc"),
            createdAtUtc: reader.GetRequiredDateTime("CreatedAtUtc"));
    }
}
