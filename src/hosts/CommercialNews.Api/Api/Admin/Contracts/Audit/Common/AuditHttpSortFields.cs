namespace CommercialNews.Api.Api.Admin.Contracts.Audit.Common;

internal static class AuditHttpSortFields
{
    public static readonly IReadOnlyDictionary<string, string> AuditLog =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["occurredAtUtc"] = "OccurredAtUtc",
            ["ingestedAtUtc"] = "IngestedAtUtc",
            ["createdAtUtc"] = "CreatedAtUtc",
            ["sourceModule"] = "SourceModule",
            ["eventType"] = "EventType",
            ["action"] = "Action",
            ["actionCategory"] = "ActionCategory",
            ["resourceType"] = "ResourceType",
            ["resourceId"] = "ResourceId",
            ["actorUserId"] = "ActorUserId",
            ["actorInternalId"] = "ActorInternalId",
            ["outcome"] = "Outcome",
            ["severity"] = "Severity",
            ["riskLevel"] = "RiskLevel"
        };

    public static readonly IReadOnlyDictionary<string, string> Ingestion =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["sourceOccurredAtUtc"] = "SourceOccurredAtUtc",
            ["sourcePublishedAtUtc"] = "SourcePublishedAtUtc",
            ["firstReceivedAtUtc"] = "FirstReceivedAtUtc",
            ["lastAttemptAtUtc"] = "LastAttemptAtUtc",
            ["processedAtUtc"] = "ProcessedAtUtc",
            ["deadLetteredAtUtc"] = "DeadLetteredAtUtc",
            ["createdAtUtc"] = "CreatedAtUtc",
            ["updatedAtUtc"] = "UpdatedAtUtc",
            ["attemptCount"] = "AttemptCount",
            ["status"] = "Status",
            ["eventType"] = "EventType",
            ["consumerName"] = "ConsumerName",
            ["lastErrorClass"] = "LastErrorClass"
        };
}
