namespace Audit.Infrastructure.Persistence.Sql;

internal static class SqlParameterNames
{
    internal static class Common
    {
        public const string PublicId = "@PublicId";
        public const string MessageId = "@MessageId";
        public const string EventType = "@EventType";
        public const string EventVersion = "@EventVersion";
        public const string SourceModule = "@SourceModule";
        public const string SourcePriority = "@SourcePriority";
        public const string SourceOccurredAtUtc = "@SourceOccurredAtUtc";
        public const string SourcePublishedAtUtc = "@SourcePublishedAtUtc";

        public const string AggregateType = "@AggregateType";
        public const string AggregateId = "@AggregateId";
        public const string AggregatePublicId = "@AggregatePublicId";
        public const string AggregateVersion = "@AggregateVersion";

        public const string CorrelationId = "@CorrelationId";
        public const string CausationId = "@CausationId";
        public const string TraceId = "@TraceId";

        public const string FromUtc = "@FromUtc";
        public const string ToUtc = "@ToUtc";
        public const string FromOccurredAtUtc = "@FromOccurredAtUtc";
        public const string ToOccurredAtUtc = "@ToOccurredAtUtc";
        public const string FromFirstReceivedAtUtc = "@FromFirstReceivedAtUtc";
        public const string ToFirstReceivedAtUtc = "@ToFirstReceivedAtUtc";
        public const string Page = "@Page";
        public const string PageSize = "@PageSize";
        public const string Skip = "@Skip";
        public const string Take = "@Take";
        public const string SortBy = "@SortBy";
        public const string SortDirection = "@SortDirection";
        public const string Limit = "@Limit";
        public const string TotalItems = "@TotalItems";
        public const string NowUtc = "@NowUtc";
    }

    internal static class AuditLog
    {
        public const string AuditLogId = "@AuditLogId";

        public const string ActorUserId = "@ActorUserId";
        public const string ActorInternalId = "@ActorInternalId";
        public const string ActorEmail = "@ActorEmail";
        public const string ActorDisplayName = "@ActorDisplayName";
        public const string ActorType = "@ActorType";

        public const string ResourceType = "@ResourceType";
        public const string ResourceId = "@ResourceId";
        public const string ResourceDisplayName = "@ResourceDisplayName";

        public const string Outcome = "@Outcome";
        public const string Severity = "@Severity";
        public const string RiskLevel = "@RiskLevel";

        public const string IpAddress = "@IpAddress";
        public const string UserAgent = "@UserAgent";

        public const string OccurredAtUtc = "@OccurredAtUtc";
        public const string Hash = "@Hash";
        public const string PrevHash = "@PrevHash";

        public const string MetadataJson = "@MetadataJson";
        public const string HeadersJson = "@HeadersJson";
        public const string SanitizedPayloadJson = "@SanitizedPayloadJson";
        public const string BeforeJson = "@BeforeJson";
        public const string AfterJson = "@AfterJson";
        public const string ChangesJson = "@ChangesJson";

        public const string Action = "@Action";
        public const string ActionCategory = "@ActionCategory";
        public const string Summary = "@Summary";
        public const string Reason = "@Reason";

        public const string IngestedAtUtc = "@IngestedAtUtc";
        public const string CreatedAtUtc = "@CreatedAtUtc";
        public const string WasInserted = "@WasInserted";
    }

    internal static class AuditIngestion
    {
        public const string AuditIngestionId = "@AuditIngestionId";

        public const string ConsumerName = "@ConsumerName";
        public const string Status = "@Status";
        public const string AttemptCount = "@AttemptCount";

        public const string LastErrorCode = "@LastErrorCode";
        public const string LastErrorMessage = "@LastErrorMessage";
        public const string LastErrorClass = "@LastErrorClass";

        public const string FirstReceivedAtUtc = "@FirstReceivedAtUtc";
        public const string LastAttemptAtUtc = "@LastAttemptAtUtc";
        public const string ProcessedAtUtc = "@ProcessedAtUtc";
        public const string DeadLetteredAtUtc = "@DeadLetteredAtUtc";

        public const string WasInserted = "@WasInserted";
        public const string CurrentStatus = "@CurrentStatus";
    }
}
