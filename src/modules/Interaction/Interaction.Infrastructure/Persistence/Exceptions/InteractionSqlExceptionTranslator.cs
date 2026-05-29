using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using Microsoft.Data.SqlClient;

namespace Interaction.Infrastructure.Persistence.Exceptions;

public sealed class InteractionSqlExceptionTranslator : SqlExceptionTranslatorBase
{
    public override Exception Translate(SqlException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.Number switch
        {
            2601 or 2627 => MapUniqueConstraint(exception),
            547 => MapForeignKeyOrCheckConstraint(exception),
            58001 or 58002
                or 58101 or 58102 or 58103 or 58104 or 58105 or 58106 or 58107 or 58108 or 58109 or 58110
                or 58201 or 58202 or 58203 or 58204 or 58205 or 58206 or 58207 or 58208 or 58209 or 58210
                or 58220 or 58221 or 58222 or 58223 or 58224 or 58225
                or 58230 or 58231 or 58232
                or 58240 or 58241 or 58242 or 58243 or 58244 or 58245 or 58246 or 58247 or 58248
                or 58250 or 58251 or 58252 or 58253 or 58254 or 58255 or 58256 or 58257 or 58258 or 58259 or 58260
                or 58270 or 58271 or 58272 or 58273 or 58274 or 58275 or 58276 or 58277
                or 58280 or 58281 or 58282 or 58283 or 58284 or 58285 or 58286 or 58287 or 58288
                or 58290 or 58291 or 58292 or 58293 or 58294 or 58295 or 58296 or 58297 or 58298 or 58299
                or 58300 or 58301 or 58302 or 58303 or 58304 or 58305 or 58306 or 58307
                or 58310 or 58311 or 58312 or 58313 or 58314 or 58315 or 58316
                or 58320 or 58321 or 58322 or 58323 or 58324 or 58325 or 58326 or 58327 or 58328 or 58329
                or 58330 or 58331 or 58332 or 58333
                or 58340 or 58341 or 58342
                or 58350 or 58351 or 58352 or 58353 or 58354 or 58355 or 58356 or 58357 or 58358 or 58359
                or 58360 or 58361 or 58362 or 58363 or 58364 or 58365 or 58366 or 58367 or 58368 or 58369
                or 58370 or 58371 or 58372
                or 58380
                or 58390 or 58391 or 58392 => MapScriptThrow(exception),

            _ => Interaction(
                "INTERACTION.UNEXPECTED_FAILURE",
                "An unexpected SQL persistence error occurred.",
                exception)
        };
    }

    private static Exception MapScriptThrow(SqlException exception)
    {
        return exception.Number switch
        {
            58001 or 58101 or 58201 => Interaction(
                "INTERACTION.DATABASE_NOT_FOUND",
                "Database CommercialNews does not exist.",
                exception),

            58002 or 58102 or 58202 => Interaction(
                "INTERACTION.SCHEMA_NOT_FOUND",
                "Schema interaction does not exist.",
                exception),

            58103 or 58104 or 58105 or 58106 or 58107 or 58108 or 58109 or 58110
                or 58203 or 58204 or 58205 or 58206 or 58207 or 58208 or 58209 or 58210 => Interaction(
                    "INTERACTION.PERSISTENCE_OBJECT_NOT_FOUND",
                    "A required Interaction persistence object does not exist.",
                    exception),

            58220 or 58221 or 58225
                or 58230 or 58231
                or 58240 or 58243 or 58246 or 58248
                or 58251 or 58256 or 58260
                or 58390 or 58391 => Interaction(
                    "INTERACTION.ARTICLE_PUBLIC_ID_REQUIRED",
                    "Article public id is required.",
                    exception),

            58222 => Interaction(
                "INTERACTION.ARTICLE_TARGET_PROJECTION_INVALID_SOURCE_STATUS",
                "Article projection source status is invalid.",
                exception),

            58223 => Interaction(
                "INTERACTION.ARTICLE_TARGET_PROJECTION_INVALID_SOURCE_VERSION",
                "Article projection source version must be greater than or equal to zero.",
                exception),

            58224 => Interaction(
                "INTERACTION.ARTICLE_TARGET_PROJECTION_INVALID_SOURCE_MESSAGE_ID",
                "Article projection source message id is invalid.",
                exception),

            58232 or 58245 or 58254 or 58330 => Interaction(
                "INTERACTION.ARTICLE_UNAVAILABLE_FOR_INTERACTION",
                "Article is not available for interaction.",
                exception),

            58257 => Interaction(
                "INTERACTION.ARTICLE_UNAVAILABLE_FOR_PUBLIC_COMMENT_QUERY",
                "Comments are not available for this article.",
                exception),

            58241 or 58244 or 58247 => Interaction(
                "INTERACTION.LIKE_INVALID_USER_ID",
                "User id must be greater than zero.",
                exception),

            58242 => Interaction(
                "INTERACTION.ARTICLE_LIKE_PUBLIC_ID_REQUIRED",
                "Article like public id is required.",
                exception),

            58250 => Interaction(
                "INTERACTION.COMMENT_PUBLIC_ID_REQUIRED",
                "Comment public id is required.",
                exception),

            58252 or 58259 or 58311 => Interaction(
                "INTERACTION.COMMENT_INVALID_AUTHOR_USER_ID",
                "Comment author user id must be greater than zero.",
                exception),

            58253 => Interaction(
                "INTERACTION.COMMENT_CONTENT_REQUIRED",
                "Comment content is required.",
                exception),

            58255 or 58270 or 58280 or 58290 or 58300 or 58310 or 58321 or 58380 => Interaction(
                "INTERACTION.COMMENT_PUBLIC_ID_REQUIRED",
                "Comment public id is required.",
                exception),

            58258 => Interaction(
                "INTERACTION.INVALID_COMMENT_STATUS_FILTER",
                "Comment status filter is invalid.",
                exception),

            58271 or 58281 or 58291 or 58301 or 58362 => Interaction(
                "INTERACTION.COMMENT_INVALID_EXPECTED_VERSION",
                "Expected comment version must be greater than or equal to one.",
                exception),

            58272 or 58282 or 58292 or 58302 or 58314 or 58352 or 58363 => Interaction(
                "INTERACTION.COMMENT_MODERATION_HISTORY_PUBLIC_ID_REQUIRED",
                "Comment moderation history public id is required.",
                exception),

            58273 or 58283 or 58293 or 58303 or 58353 or 58364 => Interaction(
                "INTERACTION.COMMENT_MODERATION_HISTORY_INVALID_ACTOR_USER_ID",
                "Moderation actor user id must be greater than zero.",
                exception),

            58274 or 58285 or 58295 or 58304 or 58312 or 58327 => Interaction(
                "INTERACTION.COMMENT_NOT_FOUND",
                "Comment was not found.",
                exception),

            58275 or 58277
                or 58286 or 58288
                or 58296 or 58299
                or 58305 or 58307
                or 58313 or 58315
                or 58368 or 58371 => Interaction(
                    "INTERACTION.COMMENT_VERSION_CONFLICT",
                    "The comment has changed. Reload the current state and try again.",
                    exception),

            58276 or 58287 or 58297 or 58306 => Interaction(
                "INTERACTION.COMMENT_INVALID_STATE_TRANSITION",
                "The requested comment transition is not allowed in its current state.",
                exception),

            58284 or 58294 or 58354 or 58365 => Interaction(
                "INTERACTION.MODERATION_REASON_CODE_REQUIRED",
                "Moderation reason code is required.",
                exception),

            58298 => Interaction(
                "INTERACTION.COMMENT_OPEN_MODERATION_CASE_REQUIRES_RESOLUTION",
                "This comment has an open moderation case and must be handled through the case-resolution workflow.",
                exception),

            58316 => Interaction(
                "INTERACTION.COMMENT_MODERATION_CASE_VERSION_CONFLICT",
                "The moderation case has changed. Reload the current state and try again.",
                exception),

            58320 => Interaction(
                "INTERACTION.COMMENT_REPORT_PUBLIC_ID_REQUIRED",
                "Comment report public id is required.",
                exception),

            58322 => Interaction(
                "INTERACTION.COMMENT_REPORT_INVALID_REPORTER_USER_ID",
                "Reporter user id must be greater than zero.",
                exception),

            58323 => Interaction(
                "INTERACTION.COMMENT_REPORT_INVALID_REASON_CODE",
                "Comment report reason code is invalid.",
                exception),

            58324 => Interaction(
                "INTERACTION.COMMENT_REPORT_DESCRIPTION_REQUIRED_FOR_OTHER_REASON",
                "Description is required when report reason code is Other.",
                exception),

            58325 => Interaction(
                "INTERACTION.COMMENT_REPORT_INVALID_SEVERITY",
                "Comment report evaluated severity is invalid.",
                exception),

            58326 => Interaction(
                "INTERACTION.COMMENT_REPORT_INVALID_ALERT_THRESHOLD",
                "Comment report normal alert threshold must be greater than zero.",
                exception),

            58328 or 58358 or 58370 => Interaction(
                "INTERACTION.COMMENT_REPORT_COMMENT_UNAVAILABLE",
                "Comment is not available for reporting.",
                exception),

            58329 => Interaction(
                "INTERACTION.COMMENT_REPORT_CANNOT_REPORT_OWN_COMMENT",
                "You cannot report your own comment.",
                exception),

            58331 => Interaction(
                "INTERACTION.COMMENT_REPORT_ALREADY_EXISTS",
                "You have already reported this comment.",
                exception),

            58332 or 58342 or 58350 or 58360 => Interaction(
                "INTERACTION.COMMENT_MODERATION_CASE_PUBLIC_ID_REQUIRED",
                "Comment moderation case public id is required.",
                exception),

            58333 => Interaction(
                "INTERACTION.COMMENT_MODERATION_CASE_ALERT_MESSAGE_ID_REQUIRED",
                "Alert message id is required when alert policy is triggered.",
                exception),

            58340 => Interaction(
                "INTERACTION.INVALID_MODERATION_CASE_STATUS_FILTER",
                "Moderation case status filter is invalid.",
                exception),

            58341 => Interaction(
                "INTERACTION.INVALID_MODERATION_CASE_PRIORITY_FILTER",
                "Moderation case priority filter is invalid.",
                exception),

            58351 or 58361 => Interaction(
                "INTERACTION.COMMENT_MODERATION_CASE_INVALID_EXPECTED_VERSION",
                "Expected moderation case version must be greater than or equal to one.",
                exception),

            58355 or 58366 => Interaction(
                "INTERACTION.COMMENT_MODERATION_CASE_NOT_FOUND",
                "Comment moderation case was not found.",
                exception),

            58356 or 58359 or 58367 or 58372 => Interaction(
                "INTERACTION.COMMENT_MODERATION_CASE_VERSION_CONFLICT",
                "The moderation case has changed. Reload the current state and try again.",
                exception),

            58357 or 58369 => Interaction(
                "INTERACTION.COMMENT_MODERATION_CASE_NOT_OPEN",
                "The moderation case is no longer open.",
                exception),

            58392 => Interaction(
                "INTERACTION.STATS_MATERIALIZATION_FAILED",
                "Interaction statistics could not be materialized.",
                exception),

            _ => Interaction(
                "INTERACTION.VALIDATION_FAILED",
                exception.Message,
                exception)
        };
    }

    private static Exception MapUniqueConstraint(SqlException exception)
    {
        string message = exception.Message;

        if (message.Contains("UQ_ArticleLike_ArticlePublicId_UserId", StringComparison.OrdinalIgnoreCase))
        {
            return Interaction(
                "INTERACTION.LIKE_ALREADY_EXISTS",
                "A like record already exists for this article and user.",
                exception);
        }

        if (message.Contains("UQ_CommentReport_CommentId_ReporterUserId", StringComparison.OrdinalIgnoreCase))
        {
            return Interaction(
                "INTERACTION.COMMENT_REPORT_ALREADY_EXISTS",
                "You have already reported this comment.",
                exception);
        }

        if (message.Contains("UX_CommentModerationCase_CommentId_Open", StringComparison.OrdinalIgnoreCase))
        {
            return Interaction(
                "INTERACTION.COMMENT_MODERATION_CASE_OPEN_ALREADY_EXISTS",
                "An open moderation case already exists for this comment.",
                exception);
        }

        if (message.Contains("UQ_ArticleInteractionTargetProjection_ArticlePublicId", StringComparison.OrdinalIgnoreCase))
        {
            return Interaction(
                "INTERACTION.ARTICLE_TARGET_PROJECTION_ALREADY_EXISTS",
                "Article interaction target projection already exists for this article.",
                exception);
        }

        if (message.Contains("UQ_ArticleViewCount_ArticlePublicId", StringComparison.OrdinalIgnoreCase))
        {
            return Interaction(
                "INTERACTION.ARTICLE_VIEW_COUNT_ALREADY_EXISTS",
                "Article view count already exists for this article.",
                exception);
        }

        if (message.Contains("UQ_ArticleInteractionStats_ArticlePublicId", StringComparison.OrdinalIgnoreCase))
        {
            return Interaction(
                "INTERACTION.STATS_ALREADY_EXISTS",
                "Interaction statistics already exist for this article.",
                exception);
        }

        if (ContainsAny(
                message,
                "UQ_ArticleLike_PublicId",
                "UQ_Comment_PublicId",
                "UQ_CommentReport_PublicId",
                "UQ_CommentModerationCase_PublicId",
                "UQ_CommentModerationActionHistory_PublicId"))
        {
            return Interaction(
                "INTERACTION.PUBLIC_ID_ALREADY_EXISTS",
                "A record with the same public id already exists.",
                exception);
        }

        if (message.Contains(
                "UQ_CommentModerationCase_CommentModerationCaseId_CommentId",
                StringComparison.OrdinalIgnoreCase))
        {
            return Interaction(
                "INTERACTION.COMMENT_MODERATION_CASE_COMMENT_MISMATCH",
                "Moderation case and comment relationship is invalid.",
                exception);
        }

        return Interaction(
            "INTERACTION.CONSTRAINT_VIOLATED",
            "A unique persistence constraint was violated.",
            exception);
    }

    private static Exception MapForeignKeyOrCheckConstraint(SqlException exception)
    {
        string message = exception.Message;

        if (message.Contains("FK_Comment_ParentComment", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("CK_Comment_ParentCommentId_V1TopLevelOnly", StringComparison.OrdinalIgnoreCase))
        {
            return Interaction(
                "INTERACTION.COMMENT_REPLY_NOT_SUPPORTED",
                "Reply comments are not supported in Interaction V1.",
                exception);
        }

        if (ContainsAny(
                message,
                "FK_CommentReport_CommentModerationCase_Comment",
                "FK_CommentModerationActionHistory_CommentModerationCase_Comment"))
        {
            return Interaction(
                "INTERACTION.COMMENT_MODERATION_CASE_NOT_FOUND",
                "Comment moderation case was not found.",
                exception);
        }

        if (ContainsAny(
                message,
                "FK_CommentReport_Comment",
                "FK_CommentModerationCase_Comment",
                "FK_CommentModerationActionHistory_Comment"))
        {
            return Interaction(
                "INTERACTION.COMMENT_NOT_FOUND",
                "Comment was not found.",
                exception);
        }

        if (ContainsAny(
                message,
                "CK_ArticleInteractionTargetProjection_ArticlePublicId_NotBlank",
                "CK_ArticleViewCount_ArticlePublicId_NotBlank",
                "CK_ArticleLike_ArticlePublicId_NotBlank",
                "CK_Comment_ArticlePublicId_NotBlank",
                "CK_ArticleInteractionStats_ArticlePublicId_NotBlank"))
        {
            return Interaction(
                "INTERACTION.ARTICLE_PUBLIC_ID_REQUIRED",
                "Article public id is required.",
                exception);
        }

        if (ContainsAny(
                message,
                "CK_ArticleInteractionTargetProjection_SourceStatus_NotBlank",
                "CK_ArticleInteractionTargetProjection_LastSourceVersion",
                "CK_ArticleInteractionTargetProjection_LastSourceMessageId_NotBlank",
                "CK_ArticleInteractionTargetProjection_UpdatedAtUtc"))
        {
            return Interaction(
                "INTERACTION.ARTICLE_TARGET_PROJECTION_INVALID_STATE",
                "Article interaction target projection state is invalid.",
                exception);
        }

        if (ContainsAny(
                message,
                "CK_ArticleViewCount_ViewCount",
                "CK_ArticleViewCount_ViewVersion",
                "CK_ArticleViewCount_UpdatedAtUtc",
                "CK_ArticleViewCount_LastAcceptedViewAtUtc"))
        {
            return Interaction(
                "INTERACTION.ARTICLE_VIEW_COUNT_INVALID_STATE",
                "Article view count state is invalid.",
                exception);
        }

        if (message.Contains("CK_ArticleLike_PublicId_NotBlank", StringComparison.OrdinalIgnoreCase))
        {
            return Interaction(
                "INTERACTION.ARTICLE_LIKE_PUBLIC_ID_REQUIRED",
                "Article like public id is required.",
                exception);
        }

        if (message.Contains("CK_ArticleLike_UserId", StringComparison.OrdinalIgnoreCase))
        {
            return Interaction(
                "INTERACTION.LIKE_INVALID_USER_ID",
                "User id must be greater than zero.",
                exception);
        }

        if (ContainsAny(
                message,
                "CK_ArticleLike_Version",
                "CK_ArticleLike_UpdatedAtUtc",
                "CK_ArticleLike_UnlikedAtUtc",
                "CK_ArticleLike_State"))
        {
            return Interaction(
                "INTERACTION.LIKE_INVALID_STATE",
                "Article like state is invalid.",
                exception);
        }

        if (message.Contains("CK_Comment_PublicId_NotBlank", StringComparison.OrdinalIgnoreCase))
        {
            return Interaction(
                "INTERACTION.COMMENT_PUBLIC_ID_REQUIRED",
                "Comment public id is required.",
                exception);
        }

        if (message.Contains("CK_Comment_AuthorUserId", StringComparison.OrdinalIgnoreCase))
        {
            return Interaction(
                "INTERACTION.COMMENT_INVALID_AUTHOR_USER_ID",
                "Comment author user id must be greater than zero.",
                exception);
        }

        if (message.Contains("CK_Comment_Content_NotBlank", StringComparison.OrdinalIgnoreCase))
        {
            return Interaction(
                "INTERACTION.COMMENT_CONTENT_REQUIRED",
                "Comment content is required.",
                exception);
        }

        if (message.Contains("CK_Comment_Status", StringComparison.OrdinalIgnoreCase))
        {
            return Interaction(
                "INTERACTION.COMMENT_INVALID_STATUS",
                "Comment status is invalid.",
                exception);
        }

        if (ContainsAny(
                message,
                "CK_Comment_Version",
                "CK_Comment_UpdatedAtUtc",
                "CK_Comment_DeletedAtUtc",
                "CK_Comment_DeletedState"))
        {
            return Interaction(
                "INTERACTION.COMMENT_INVALID_STATE",
                "Comment state is invalid.",
                exception);
        }

        if (message.Contains("CK_CommentReport_PublicId_NotBlank", StringComparison.OrdinalIgnoreCase))
        {
            return Interaction(
                "INTERACTION.COMMENT_REPORT_PUBLIC_ID_REQUIRED",
                "Comment report public id is required.",
                exception);
        }

        if (message.Contains("CK_CommentReport_ReporterUserId", StringComparison.OrdinalIgnoreCase))
        {
            return Interaction(
                "INTERACTION.COMMENT_REPORT_INVALID_REPORTER_USER_ID",
                "Reporter user id must be greater than zero.",
                exception);
        }

        if (message.Contains("CK_CommentReport_ReasonCode", StringComparison.OrdinalIgnoreCase))
        {
            return Interaction(
                "INTERACTION.COMMENT_REPORT_INVALID_REASON_CODE",
                "Comment report reason code is invalid.",
                exception);
        }

        if (message.Contains("CK_CommentReport_OtherDescriptionRequired", StringComparison.OrdinalIgnoreCase))
        {
            return Interaction(
                "INTERACTION.COMMENT_REPORT_DESCRIPTION_REQUIRED_FOR_OTHER_REASON",
                "Description is required when report reason code is Other.",
                exception);
        }

        if (message.Contains("CK_CommentReport_Description_NotBlank", StringComparison.OrdinalIgnoreCase))
        {
            return Interaction(
                "INTERACTION.COMMENT_REPORT_INVALID_DESCRIPTION",
                "Comment report description must not be blank when provided.",
                exception);
        }

        if (ContainsAny(
                message,
                "CK_CommentReport_Status",
                "CK_CommentReport_ResolutionState",
                "CK_CommentReport_ResolvedAtUtc"))
        {
            return Interaction(
                "INTERACTION.COMMENT_REPORT_INVALID_STATE",
                "Comment report state is invalid.",
                exception);
        }

        if (message.Contains("CK_CommentModerationCase_PublicId_NotBlank", StringComparison.OrdinalIgnoreCase))
        {
            return Interaction(
                "INTERACTION.COMMENT_MODERATION_CASE_PUBLIC_ID_REQUIRED",
                "Comment moderation case public id is required.",
                exception);
        }

        if (message.Contains("CK_CommentModerationCase_Status", StringComparison.OrdinalIgnoreCase))
        {
            return Interaction(
                "INTERACTION.INVALID_MODERATION_CASE_STATUS_FILTER",
                "Moderation case status is invalid.",
                exception);
        }

        if (message.Contains("CK_CommentModerationCase_Priority", StringComparison.OrdinalIgnoreCase))
        {
            return Interaction(
                "INTERACTION.INVALID_MODERATION_CASE_PRIORITY_FILTER",
                "Moderation case priority is invalid.",
                exception);
        }

        if (ContainsAny(
                message,
                "CK_CommentModerationCase_HighestSeverity",
                "CK_CommentModerationCase_AlertLevel",
                "CK_CommentModerationCase_AlertState",
                "CK_CommentModerationCase_ResolutionType",
                "CK_CommentModerationCase_ResolutionReasonCode",
                "CK_CommentModerationCase_ReasonRequiredByResolutionType",
                "CK_CommentModerationCase_ResolutionState",
                "CK_CommentModerationCase_ResolutionNote_NotBlank",
                "CK_CommentModerationCase_OtherResolutionNoteRequired",
                "CK_CommentModerationCase_ResolvedAtUtc",
                "CK_CommentModerationCase_AlertTriggeredAtUtc",
                "CK_CommentModerationCase_Version"))
        {
            return Interaction(
                "INTERACTION.COMMENT_MODERATION_CASE_INVALID_STATE",
                "Comment moderation case state is invalid.",
                exception);
        }

        if (message.Contains("CK_CommentModerationActionHistory_PublicId_NotBlank", StringComparison.OrdinalIgnoreCase))
        {
            return Interaction(
                "INTERACTION.COMMENT_MODERATION_HISTORY_PUBLIC_ID_REQUIRED",
                "Comment moderation history public id is required.",
                exception);
        }

        if (ContainsAny(
                message,
                "CK_CommentModerationActionHistory_ActionType",
                "CK_CommentModerationActionHistory_CaseRequiredByActionType",
                "CK_CommentModerationActionHistory_FromStatus",
                "CK_CommentModerationActionHistory_ToStatus",
                "CK_CommentModerationActionHistory_ActorType_NotBlank",
                "CK_CommentModerationActionHistory_ReasonCode",
                "CK_CommentModerationActionHistory_ReasonRequiredByActionType",
                "CK_CommentModerationActionHistory_Note_NotBlank",
                "CK_CommentModerationActionHistory_OtherNoteRequired",
                "CK_CommentModerationActionHistory_CorrelationId_NotBlank"))
        {
            return Interaction(
                "INTERACTION.COMMENT_MODERATION_HISTORY_INVALID_STATE",
                "Comment moderation history state is invalid.",
                exception);
        }

        if (ContainsAny(
                message,
                "CK_ArticleInteractionStats_ViewCount",
                "CK_ArticleInteractionStats_LikeCount",
                "CK_ArticleInteractionStats_VisibleCommentCount",
                "CK_ArticleInteractionStats_StatsVersion",
                "CK_ArticleInteractionStats_LastPublishedMessageId_NotBlank",
                "CK_ArticleInteractionStats_UpdatedAtUtc",
                "CK_ArticleInteractionStats_LastMaterializedAtUtc",
                "CK_ArticleInteractionStats_LastPublishedAtUtc",
                "CK_ArticleInteractionStats_PublicationState"))
        {
            return Interaction(
                "INTERACTION.STATS_INVALID_STATE",
                "Interaction statistics state is invalid.",
                exception);
        }

        return Interaction(
            "INTERACTION.CONSTRAINT_VIOLATED",
            "A foreign key or check constraint was violated.",
            exception);
    }

    private static bool ContainsAny(string source, params string[] values)
    {
        foreach (string value in values)
        {
            if (source.Contains(value, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static InteractionPersistenceException Interaction(
        string code,
        string message,
        Exception exception)
    {
        return new InteractionPersistenceException(
            code: code,
            message: message,
            innerException: exception);
    }
}
