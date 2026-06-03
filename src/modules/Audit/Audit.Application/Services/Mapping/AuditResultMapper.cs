using Audit.Application.Abstractions.Persistence.Results;
using Audit.Application.Models.Results.AuditLogs;
using Audit.Application.Models.Results.Dashboard;
using Audit.Application.Models.Results.Ingestion;
using Audit.Domain.Entities;

namespace Audit.Application.Services.Mapping;

public sealed class AuditResultMapper : IAuditResultMapper
{
    public AuditLogListItemResult ToAuditLogListItem(
        AuditLog auditLog)
    {
        ArgumentNullException.ThrowIfNull(auditLog);

        return new AuditLogListItemResult(
            PublicId: auditLog.PublicId,
            MessageId: auditLog.SourceEvent.MessageId,
            EventType: auditLog.SourceEvent.EventType,
            SourceModule: auditLog.SourceEvent.SourceModule,
            Action: auditLog.Action,
            ActionCategory: auditLog.ActionCategory,
            ResourceType: auditLog.Resource.ResourceType,
            ResourceId: auditLog.Resource.ResourceId,
            ResourceDisplayName: auditLog.Resource.ResourceDisplayName,
            ActorInternalId: auditLog.Actor.ActorInternalId,
            ActorUserId: auditLog.Actor.ActorUserId,
            ActorEmail: auditLog.Actor.ActorEmail,
            ActorDisplayName: auditLog.Actor.ActorDisplayName,
            ActorType: auditLog.Actor.ActorType,
            Outcome: auditLog.Risk.Outcome,
            Severity: auditLog.Risk.Severity,
            RiskLevel: auditLog.Risk.RiskLevel,
            Summary: auditLog.Summary,
            CorrelationId: auditLog.TraceContext.CorrelationId,
            OccurredAtUtc: auditLog.OccurredAtUtc,
            IngestedAtUtc: auditLog.IngestedAtUtc);
    }

    public AuditLogDetailResult ToAuditLogDetail(
        AuditLog auditLog)
    {
        ArgumentNullException.ThrowIfNull(auditLog);

        return new AuditLogDetailResult(
            PublicId: auditLog.PublicId,
            MessageId: auditLog.SourceEvent.MessageId,
            EventType: auditLog.SourceEvent.EventType,
            EventVersion: auditLog.SourceEvent.EventVersion,
            SourceModule: auditLog.SourceEvent.SourceModule,
            Action: auditLog.Action,
            ActionCategory: auditLog.ActionCategory,
            Aggregate: new AuditAggregateResult(
                AggregateType: auditLog.AggregateRef.AggregateType,
                AggregateId: auditLog.AggregateRef.AggregateId,
                AggregatePublicId: auditLog.AggregateRef.AggregatePublicId,
                AggregateVersion: auditLog.AggregateRef.AggregateVersion),
            Actor: new AuditActorResult(
                ActorInternalId: auditLog.Actor.ActorInternalId,
                ActorUserId: auditLog.Actor.ActorUserId,
                ActorEmail: auditLog.Actor.ActorEmail,
                ActorDisplayName: auditLog.Actor.ActorDisplayName,
                ActorType: auditLog.Actor.ActorType),
            Resource: new AuditResourceResult(
                ResourceType: auditLog.Resource.ResourceType,
                ResourceId: auditLog.Resource.ResourceId,
                ResourceDisplayName: auditLog.Resource.ResourceDisplayName),
            Outcome: auditLog.Risk.Outcome,
            Severity: auditLog.Risk.Severity,
            RiskLevel: auditLog.Risk.RiskLevel,
            Summary: auditLog.Summary,
            Reason: auditLog.Reason,
            CorrelationId: auditLog.TraceContext.CorrelationId,
            CausationId: auditLog.TraceContext.CausationId,
            TraceId: auditLog.TraceContext.TraceId,
            IpAddress: auditLog.RequestContext.IpAddress,
            UserAgent: auditLog.RequestContext.UserAgent,
            SourcePriority: auditLog.SourcePriority,
            OccurredAtUtc: auditLog.OccurredAtUtc,
            IngestedAtUtc: auditLog.IngestedAtUtc,
            CreatedAtUtc: auditLog.CreatedAtUtc,
            MetadataJson: auditLog.JsonPayload.MetadataJson,
            HeadersJson: auditLog.JsonPayload.HeadersJson,
            SanitizedPayloadJson: auditLog.JsonPayload.SanitizedPayloadJson,
            BeforeJson: auditLog.JsonPayload.BeforeJson,
            AfterJson: auditLog.JsonPayload.AfterJson,
            ChangesJson: auditLog.JsonPayload.ChangesJson);
    }

    public AuditIngestionListItemResult ToAuditIngestionListItem(
        AuditIngestion ingestion)
    {
        ArgumentNullException.ThrowIfNull(ingestion);

        return new AuditIngestionListItemResult(
            PublicId: ingestion.PublicId,
            MessageId: ingestion.SourceEvent.MessageId,
            EventType: ingestion.SourceEvent.EventType,
            AggregateType: ingestion.AggregateRef.AggregateType,
            AggregateId: ingestion.AggregateRef.AggregateId,
            AggregatePublicId: ingestion.AggregateRef.AggregatePublicId,
            AggregateVersion: ingestion.AggregateRef.AggregateVersion,
            CorrelationId: ingestion.TraceContext.CorrelationId,
            SourcePriority: ingestion.SourcePriority,
            SourceOccurredAtUtc: ingestion.SourceOccurredAtUtc,
            SourcePublishedAtUtc: ingestion.SourcePublishedAtUtc,
            ConsumerName: ingestion.ConsumerName,
            Status: ingestion.Status,
            AttemptCount: ingestion.AttemptCount,
            FirstReceivedAtUtc: ingestion.FirstReceivedAtUtc,
            LastAttemptAtUtc: ingestion.LastAttemptAtUtc,
            ProcessedAtUtc: ingestion.ProcessedAtUtc,
            DeadLetteredAtUtc: ingestion.DeadLetteredAtUtc,
            LastErrorCode: ingestion.ErrorInfo.LastErrorCode,
            LastErrorClass: ingestion.ErrorInfo.LastErrorClass,
            CreatedAtUtc: ingestion.CreatedAtUtc,
            UpdatedAtUtc: ingestion.UpdatedAtUtc);
    }

    public AuditIngestionDetailResult ToAuditIngestionDetail(
        AuditIngestion ingestion)
    {
        ArgumentNullException.ThrowIfNull(ingestion);

        return new AuditIngestionDetailResult(
            PublicId: ingestion.PublicId,
            MessageId: ingestion.SourceEvent.MessageId,
            EventType: ingestion.SourceEvent.EventType,
            AggregateType: ingestion.AggregateRef.AggregateType,
            AggregateId: ingestion.AggregateRef.AggregateId,
            AggregatePublicId: ingestion.AggregateRef.AggregatePublicId,
            AggregateVersion: ingestion.AggregateRef.AggregateVersion,
            CorrelationId: ingestion.TraceContext.CorrelationId,
            SourcePriority: ingestion.SourcePriority,
            SourceOccurredAtUtc: ingestion.SourceOccurredAtUtc,
            SourcePublishedAtUtc: ingestion.SourcePublishedAtUtc,
            ConsumerName: ingestion.ConsumerName,
            Status: ingestion.Status,
            AttemptCount: ingestion.AttemptCount,
            FirstReceivedAtUtc: ingestion.FirstReceivedAtUtc,
            LastAttemptAtUtc: ingestion.LastAttemptAtUtc,
            ProcessedAtUtc: ingestion.ProcessedAtUtc,
            DeadLetteredAtUtc: ingestion.DeadLetteredAtUtc,
            LastErrorCode: ingestion.ErrorInfo.LastErrorCode,
            LastErrorMessage: ingestion.ErrorInfo.LastErrorMessage,
            LastErrorClass: ingestion.ErrorInfo.LastErrorClass,
            CreatedAtUtc: ingestion.CreatedAtUtc,
            UpdatedAtUtc: ingestion.UpdatedAtUtc);
    }

    public RecentRiskAuditEventResult ToRecentRiskEvent(
        AuditLog auditLog)
    {
        ArgumentNullException.ThrowIfNull(auditLog);

        return new RecentRiskAuditEventResult(
            PublicId: auditLog.PublicId,
            MessageId: auditLog.SourceEvent.MessageId,
            EventType: auditLog.SourceEvent.EventType,
            SourceModule: auditLog.SourceEvent.SourceModule,
            Action: auditLog.Action,
            ActionCategory: auditLog.ActionCategory,
            ResourceType: auditLog.Resource.ResourceType,
            ResourceId: auditLog.Resource.ResourceId,
            ResourceDisplayName: auditLog.Resource.ResourceDisplayName,
            ActorInternalId: auditLog.Actor.ActorInternalId,
            ActorUserId: auditLog.Actor.ActorUserId,
            ActorEmail: auditLog.Actor.ActorEmail,
            ActorDisplayName: auditLog.Actor.ActorDisplayName,
            ActorType: auditLog.Actor.ActorType,
            Outcome: auditLog.Risk.Outcome,
            Severity: auditLog.Risk.Severity,
            RiskLevel: auditLog.Risk.RiskLevel,
            Summary: auditLog.Summary,
            CorrelationId: auditLog.TraceContext.CorrelationId,
            OccurredAtUtc: auditLog.OccurredAtUtc,
            IngestedAtUtc: auditLog.IngestedAtUtc);
    }

    public AuditDashboardCountByModuleResult ToModuleCount(
        AuditCountByValueResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new AuditDashboardCountByModuleResult(
            SourceModule: result.Value,
            Count: result.Count);
    }

    public AuditDashboardCountBySeverityResult ToSeverityCount(
        AuditCountByValueResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new AuditDashboardCountBySeverityResult(
            Severity: result.Value,
            Count: result.Count);
    }

    public AuditDashboardCountByRiskLevelResult ToRiskLevelCount(
        AuditCountByValueResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new AuditDashboardCountByRiskLevelResult(
            RiskLevel: result.Value,
            Count: result.Count);
    }

    public AuditDashboardSummaryResult ToDashboardSummary(
        AuditDashboardSummaryDataResult data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return new AuditDashboardSummaryResult(
            Window: new AuditDashboardWindowResult(
                FromUtc: data.FromUtc,
                ToUtc: data.ToUtc),
            Totals: new AuditDashboardTotalsResult(
                AuditEvents: data.AuditEvents,
                HighRiskEvents: data.HighRiskEvents,
                CriticalEvents: data.CriticalEvents,
                FailedIngestion: data.FailedIngestion,
                DuplicateIngestion: data.DuplicateIngestion),
            ByModule: data.CountsByModule
                .Select(ToModuleCount)
                .ToArray(),
            BySeverity: data.CountsBySeverity
                .Select(ToSeverityCount)
                .ToArray(),
            ByRiskLevel: data.CountsByRiskLevel
                .Select(ToRiskLevelCount)
                .ToArray(),
            Freshness: new AuditDashboardFreshnessResult(
                GeneratedAtUtc: data.GeneratedAtUtc,
                OldestFailedIngestionAgeSeconds: data.OldestFailedIngestionAgeSeconds));
    }
}
