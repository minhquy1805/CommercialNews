namespace Audit.Infrastructure.Persistence.Sql;

internal static class SqlStoredProcedures
{
    internal static class AuditLog
    {
        public const string Insert = "audit.AuditLog_Insert";
        public const string SelectById = "audit.AuditLog_SelectById";
        public const string SelectByPublicId = "audit.AuditLog_SelectByPublicId";
        public const string SelectByMessageId = "audit.AuditLog_SelectByMessageId";
        public const string SelectByCorrelationId = "audit.AuditLog_SelectByCorrelationId";
        public const string SelectByResource = "audit.AuditLog_SelectByResource";
        public const string SelectByActorUserId = "audit.AuditLog_SelectByActorUserId";
        public const string SelectAll = "audit.AuditLog_SelectAll";
        public const string SelectSkipAndTake = "audit.AuditLog_SelectSkipAndTake";
        public const string SelectSkipAndTakeWhereDynamic = "audit.AuditLog_SelectSkipAndTakeWhereDynamic";
        public const string GetRecordCount = "audit.AuditLog_GetRecordCount";
        public const string GetRecordCountWhereDynamic = "audit.AuditLog_GetRecordCountWhereDynamic";
        public const string SelectRecentRiskEvents = "audit.AuditLog_SelectRecentRiskEvents";
        public const string CountByModule = "audit.AuditLog_CountByModule";
        public const string CountBySeverity = "audit.AuditLog_CountBySeverity";
        public const string CountByRiskLevel = "audit.AuditLog_CountByRiskLevel";
    }

    internal static class AuditIngestion
    {
        public const string UpsertProcessing = "audit.AuditIngestion_UpsertProcessing";
        public const string UpsertUnsupported = "audit.AuditIngestion_UpsertUnsupported";

        public const string MarkSucceeded = "audit.AuditIngestion_MarkSucceeded";
        public const string MarkDuplicate = "audit.AuditIngestion_MarkDuplicate";
        public const string MarkIgnored = "audit.AuditIngestion_MarkIgnored";
        public const string MarkFailed = "audit.AuditIngestion_MarkFailed";
        public const string MarkDeadLettered = "audit.AuditIngestion_MarkDeadLettered";

        public const string SelectById = "audit.AuditIngestion_SelectById";
        public const string SelectByPublicId = "audit.AuditIngestion_SelectByPublicId";
        public const string SelectByMessageId = "audit.AuditIngestion_SelectByMessageId";
        public const string SelectFailed = "audit.AuditIngestion_SelectFailed";
        public const string SelectSkipAndTakeWhereDynamic = "audit.AuditIngestion_SelectSkipAndTakeWhereDynamic";
        public const string SelectFailedWhereDynamic = "audit.AuditIngestion_SelectFailedWhereDynamic";
        public const string GetRecordCountWhereDynamic = "audit.AuditIngestion_GetRecordCountWhereDynamic";
        public const string GetFailedRecordCountWhereDynamic = "audit.AuditIngestion_GetFailedRecordCountWhereDynamic";
        public const string GetRecordCount = "audit.AuditIngestion_GetRecordCount";
        public const string CountFailedForDashboard = "audit.AuditIngestion_CountFailedForDashboard";
        public const string CountDuplicateForDashboard = "audit.AuditIngestion_CountDuplicateForDashboard";
        public const string GetOldestFailedIngestionAgeSeconds = "audit.AuditIngestion_GetOldestFailedIngestionAgeSeconds";
    }
}
