using Audit.Application.Models.Results.AuditLogs;
using Audit.Application.Models.Results.Dashboard;
using CommercialNews.Api.Api.Admin.Contracts.Audit.AuditLog.Responses;
using CommercialNews.Api.Api.Admin.Contracts.Audit.Common;

namespace CommercialNews.Api.Api.Admin.Contracts.Audit.AuditLog.Mapping;

internal static class AuditLogHttpMapper
{
    public static AuditLogListItemHttpResponse ToListItem(
        AuditLogListItemResult item)
    {
        return new AuditLogListItemHttpResponse
        {
            PublicId = item.PublicId,
            MessageId = item.MessageId,
            EventType = item.EventType,
            SourceModule = item.SourceModule,
            Action = item.Action,
            ActionCategory = item.ActionCategory,
            Actor = new AuditActorHttpResponse
            {
                ActorInternalId = item.ActorInternalId,
                ActorUserId = item.ActorUserId,
                ActorEmail = item.ActorEmail,
                ActorDisplayName = item.ActorDisplayName,
                ActorType = item.ActorType
            },
            Resource = new AuditResourceHttpResponse
            {
                Type = item.ResourceType,
                Id = item.ResourceId,
                DisplayName = item.ResourceDisplayName
            },
            Outcome = item.Outcome,
            Severity = item.Severity,
            RiskLevel = item.RiskLevel,
            Summary = item.Summary,
            CorrelationId = item.CorrelationId,
            OccurredAtUtc = item.OccurredAtUtc,
            IngestedAtUtc = item.IngestedAtUtc
        };
    }

    public static AuditLogListItemHttpResponse ToListItem(
        RecentRiskAuditEventResult item)
    {
        return new AuditLogListItemHttpResponse
        {
            PublicId = item.PublicId,
            MessageId = item.MessageId,
            EventType = item.EventType,
            SourceModule = item.SourceModule,
            Action = item.Action,
            ActionCategory = item.ActionCategory,
            Actor = new AuditActorHttpResponse
            {
                ActorInternalId = item.ActorInternalId,
                ActorUserId = item.ActorUserId,
                ActorEmail = item.ActorEmail,
                ActorDisplayName = item.ActorDisplayName,
                ActorType = item.ActorType
            },
            Resource = new AuditResourceHttpResponse
            {
                Type = item.ResourceType,
                Id = item.ResourceId,
                DisplayName = item.ResourceDisplayName
            },
            Outcome = item.Outcome,
            Severity = item.Severity,
            RiskLevel = item.RiskLevel,
            Summary = item.Summary,
            CorrelationId = item.CorrelationId,
            OccurredAtUtc = item.OccurredAtUtc,
            IngestedAtUtc = item.IngestedAtUtc
        };
    }

    public static GetAuditLogByIdHttpResponse ToDetail(
        AuditLogDetailResult item)
    {
        return new GetAuditLogByIdHttpResponse
        {
            PublicId = item.PublicId,
            MessageId = item.MessageId,
            EventType = item.EventType,
            EventVersion = item.EventVersion,
            SourceModule = item.SourceModule,
            Action = item.Action,
            ActionCategory = item.ActionCategory,
            Aggregate = new AuditAggregateHttpResponse
            {
                Type = item.Aggregate.AggregateType,
                Id = item.Aggregate.AggregateId,
                PublicId = item.Aggregate.AggregatePublicId,
                Version = item.Aggregate.AggregateVersion
            },
            Actor = new AuditActorHttpResponse
            {
                ActorInternalId = item.Actor.ActorInternalId,
                ActorUserId = item.Actor.ActorUserId,
                ActorEmail = item.Actor.ActorEmail,
                ActorDisplayName = item.Actor.ActorDisplayName,
                ActorType = item.Actor.ActorType
            },
            Resource = new AuditResourceHttpResponse
            {
                Type = item.Resource.ResourceType,
                Id = item.Resource.ResourceId,
                DisplayName = item.Resource.ResourceDisplayName
            },
            Outcome = item.Outcome,
            Severity = item.Severity,
            RiskLevel = item.RiskLevel,
            Summary = item.Summary,
            Reason = item.Reason,
            CorrelationId = item.CorrelationId,
            CausationId = item.CausationId,
            TraceId = item.TraceId,
            IpAddress = item.IpAddress,
            UserAgent = item.UserAgent,
            SourcePriority = item.SourcePriority,
            OccurredAtUtc = item.OccurredAtUtc,
            IngestedAtUtc = item.IngestedAtUtc,
            CreatedAtUtc = item.CreatedAtUtc,
            MetadataJson = item.MetadataJson,
            HeadersJson = item.HeadersJson,
            SanitizedPayloadJson = item.SanitizedPayloadJson,
            BeforeJson = item.BeforeJson,
            AfterJson = item.AfterJson,
            ChangesJson = item.ChangesJson
        };
    }
}
