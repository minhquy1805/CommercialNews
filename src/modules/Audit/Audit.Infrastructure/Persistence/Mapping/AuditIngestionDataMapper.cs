using Audit.Domain.Entities;
using Audit.Domain.Constants.Events;
using Audit.Domain.ValueObjects.Common;
using Audit.Domain.ValueObjects.Ingestion;
using Audit.Infrastructure.Persistence.Sql;
using Microsoft.Data.SqlClient;

namespace Audit.Infrastructure.Persistence.Mapping;

internal static class AuditIngestionDataMapper
{
    public static AuditIngestion Map(
        SqlDataReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var sourceEvent = AuditSourceEvent.Create(
            messageId: reader.GetRequiredString("MessageId"),
            eventType: reader.GetRequiredString("EventType"),
            eventVersion: null,
            sourceModule: ResolveSourceModule(reader.GetRequiredString("EventType")),
            sourcePriority: reader.GetNullableInt32("SourcePriority"),
            sourceOccurredAtUtc: reader.GetRequiredDateTime("SourceOccurredAtUtc"),
            sourcePublishedAtUtc: reader.GetNullableDateTime("SourcePublishedAtUtc"));

        var aggregateRef = AuditAggregateRef.Create(
            aggregateType: reader.GetNullableString("AggregateType"),
            aggregateId: reader.GetNullableString("AggregateId"),
            aggregatePublicId: reader.GetNullableString("AggregatePublicId"),
            aggregateVersion: reader.GetNullableInt32("AggregateVersion"));

        var traceContext = AuditTraceContext.Create(
            correlationId: reader.GetNullableString("CorrelationId"),
            causationId: null,
            traceId: null);

        var errorInfo = BuildErrorInfo(reader);

        return AuditIngestion.Rehydrate(
            auditIngestionId: reader.GetRequiredInt64("AuditIngestionId"),
            publicId: reader.GetRequiredString("PublicId"),
            sourceEvent: sourceEvent,
            aggregateRef: aggregateRef,
            traceContext: traceContext,
            consumerName: reader.GetRequiredString("ConsumerName"),
            status: reader.GetRequiredString("Status"),
            attemptCount: reader.GetRequiredInt32("AttemptCount"),
            errorInfo: errorInfo,
            firstReceivedAtUtc: reader.GetRequiredDateTime("FirstReceivedAtUtc"),
            createdAtUtc: reader.GetRequiredDateTime("CreatedAtUtc"),
            updatedAtUtc: reader.GetRequiredDateTime("UpdatedAtUtc"),
            lastAttemptAtUtc: reader.GetNullableDateTime("LastAttemptAtUtc"),
            processedAtUtc: reader.GetNullableDateTime("ProcessedAtUtc"),
            deadLetteredAtUtc: reader.GetNullableDateTime("DeadLetteredAtUtc"));
    }

    private static AuditErrorInfo BuildErrorInfo(
        SqlDataReader reader)
    {
        var lastErrorCode = reader.GetNullableString("LastErrorCode");
        var lastErrorMessage = reader.GetNullableString("LastErrorMessage");
        var lastErrorClass = reader.GetNullableString("LastErrorClass");

        if (string.IsNullOrWhiteSpace(lastErrorCode) &&
            string.IsNullOrWhiteSpace(lastErrorMessage) &&
            string.IsNullOrWhiteSpace(lastErrorClass))
        {
            return AuditErrorInfo.None();
        }

        return AuditErrorInfo.Create(
            lastErrorCode: lastErrorCode,
            lastErrorMessage: lastErrorMessage,
            lastErrorClass: lastErrorClass);
    }

    private static string ResolveSourceModule(
        string eventType)
    {
        if (eventType.StartsWith("authorization.", StringComparison.OrdinalIgnoreCase) ||
            AuditEventTypes.IsAuthorizationEvent(eventType))
        {
            return AuditSourceModules.Authorization;
        }

        if (eventType.StartsWith("identity.", StringComparison.OrdinalIgnoreCase) ||
            AuditEventTypes.IsIdentityEvent(eventType))
        {
            return AuditSourceModules.Identity;
        }

        if (eventType.StartsWith("content.", StringComparison.OrdinalIgnoreCase) ||
            AuditEventTypes.IsContentEvent(eventType))
        {
            return AuditSourceModules.Content;
        }

        if (eventType.StartsWith("media.", StringComparison.OrdinalIgnoreCase) ||
            AuditEventTypes.IsMediaEvent(eventType))
        {
            return AuditSourceModules.Media;
        }

        if (eventType.StartsWith("interaction.", StringComparison.OrdinalIgnoreCase) ||
            AuditEventTypes.IsInteractionEvent(eventType))
        {
            return AuditSourceModules.Interaction;
        }

        if (eventType.StartsWith("seo.", StringComparison.OrdinalIgnoreCase))
        {
            return AuditSourceModules.Seo;
        }

        if (eventType.StartsWith("notifications.", StringComparison.OrdinalIgnoreCase))
        {
            return AuditSourceModules.Notifications;
        }

        if (eventType.StartsWith("audit.", StringComparison.OrdinalIgnoreCase))
        {
            return AuditSourceModules.Audit;
        }

        return AuditSourceModules.System;
    }
}
