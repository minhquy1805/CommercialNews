using Audit.Application.Models.Queries.AuditLogs;
using CommercialNews.Api.Api.Admin.Contracts.Audit.AuditLog.Requests;
using CommercialNews.Api.Api.Admin.Contracts.Audit.Common;

namespace CommercialNews.Api.Api.Admin.Contracts.Audit.AuditLog.Mapping;

internal static class AuditLogQueryMapper
{
    public static GetAuditLogListQuery ToQuery(
        GetAuditLogsHttpRequest request,
        string? sourceModuleOverride,
        string? resourceTypeOverride,
        string? resourceIdOverride,
        string? actorUserIdOverride)
    {
        var sort = AuditHttpSortParser.Parse(
            request.Sort,
            AuditHttpSortFields.AuditLog);

        return new GetAuditLogListQuery(
            MessageId: request.MessageId,
            SourceModule: sourceModuleOverride ?? request.SourceModule,
            EventType: request.EventType,
            Action: request.Action,
            ActionCategory: request.ActionCategory,
            ResourceType: resourceTypeOverride ?? request.ResourceType,
            ResourceId: resourceIdOverride ?? request.ResourceId,
            ActorUserId: actorUserIdOverride ?? request.ActorUserId,
            ActorInternalId: request.ActorInternalId,
            Outcome: request.Outcome,
            Severity: request.Severity,
            RiskLevel: request.RiskLevel,
            CorrelationId: request.CorrelationId,
            FromUtc: request.FromUtc,
            ToUtc: request.ToUtc,
            Page: request.Page,
            PageSize: request.PageSize,
            SortBy: sort.SortBy,
            SortDirection: sort.SortDirection);
    }

    public static GetAuditLogListQuery ToQuery(
        GetAuditTimelineHttpRequest request,
        string? sourceModuleOverride,
        string? resourceTypeOverride,
        string? resourceIdOverride,
        string? actorUserIdOverride)
    {
        var sort = AuditHttpSortParser.Parse(
            request.Sort,
            AuditHttpSortFields.AuditLog);

        return new GetAuditLogListQuery(
            MessageId: null,
            SourceModule: sourceModuleOverride ?? request.SourceModule,
            EventType: null,
            Action: null,
            ActionCategory: null,
            ResourceType: resourceTypeOverride,
            ResourceId: resourceIdOverride,
            ActorUserId: actorUserIdOverride,
            ActorInternalId: null,
            Outcome: null,
            Severity: null,
            RiskLevel: request.RiskLevel,
            CorrelationId: null,
            FromUtc: request.FromUtc,
            ToUtc: request.ToUtc,
            Page: request.Page,
            PageSize: request.PageSize,
            SortBy: sort.SortBy,
            SortDirection: sort.SortDirection);
    }
}
