namespace Audit.Application.Models.Results.Dashboard;

public sealed record RecentRiskAuditEventResult(
    string PublicId,
    string MessageId,
    string EventType,
    string SourceModule,
    string Action,
    string? ActionCategory,
    string ResourceType,
    string ResourceId,
    string? ResourceDisplayName,
    long? ActorInternalId,
    string? ActorUserId,
    string? ActorEmail,
    string? ActorDisplayName,
    string ActorType,
    string Outcome,
    string Severity,
    string RiskLevel,
    string Summary,
    string? CorrelationId,
    DateTime OccurredAtUtc,
    DateTime IngestedAtUtc);