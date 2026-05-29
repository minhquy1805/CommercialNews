using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Interaction.Application.Errors;

public static class InteractionErrors
{
    public static readonly Error ValidationFailed =
        Error.Validation(
            code: "INTERACTION.VALIDATION_FAILED",
            message: "One or more interaction validations failed.");

    public static readonly Error RateLimited =
        Error.RateLimited(
            code: "INTERACTION.RATE_LIMITED",
            message: "Too many interaction requests. Please try again later.");

    public static readonly Error UnexpectedFailure =
        Error.Failure(
            code: "INTERACTION.UNEXPECTED_FAILURE",
            message: "An unexpected interaction error occurred.");

    public static class Article
    {
        public static readonly Error ArticlePublicIdRequired =
            Error.Validation(
                code: "INTERACTION.ARTICLE_PUBLIC_ID_REQUIRED",
                message: "Article public id is required.");

        /// <summary>
        /// Public/user-facing unavailable-resource posture.
        /// Used when an article is missing, unpublished, disabled for interaction
        /// or cannot be safely exposed as interactable.
        /// </summary>
        public static readonly Error UnavailableForInteraction =
            Error.NotFound(
                code: "INTERACTION.ARTICLE_UNAVAILABLE_FOR_INTERACTION",
                message: "Article is not available for interaction.");

        public static readonly Error UnavailableForPublicCommentQuery =
            Error.NotFound(
                code: "INTERACTION.ARTICLE_UNAVAILABLE_FOR_PUBLIC_COMMENT_QUERY",
                message: "Comments are not available for this article.");

        public static readonly Error InvalidArticlePublicId =
            Error.Validation(
                code: "INTERACTION.INVALID_ARTICLE_PUBLIC_ID",
                message: "Article public id is invalid.");
    }

    public static class ArticleInteractionTargetProjection
    {
        public static readonly Error InvalidSourceStatus =
            Error.Validation(
                code: "INTERACTION.ARTICLE_TARGET_PROJECTION_INVALID_SOURCE_STATUS",
                message: "Article projection source status is invalid.");

        public static readonly Error InvalidSourceVersion =
            Error.Validation(
                code: "INTERACTION.ARTICLE_TARGET_PROJECTION_INVALID_SOURCE_VERSION",
                message: "Article projection source version must be greater than or equal to zero.");

        public static readonly Error InvalidSourceMessageId =
            Error.Validation(
                code: "INTERACTION.ARTICLE_TARGET_PROJECTION_INVALID_SOURCE_MESSAGE_ID",
                message: "Article projection source message id is invalid.");

        public static readonly Error InvalidSourceOccurredAtUtc =
            Error.Validation(
                code: "INTERACTION.ARTICLE_TARGET_PROJECTION_INVALID_SOURCE_OCCURRED_AT_UTC",
                message: "Article projection source occurred time is invalid.");

        public static readonly Error RequiresResync =
            Error.Failure(
                code: "INTERACTION.ARTICLE_TARGET_PROJECTION_REQUIRES_RESYNC",
                message: "Article interaction eligibility projection requires resynchronization.");

        public static readonly Error ApplyFailed =
            Error.Failure(
                code: "INTERACTION.ARTICLE_TARGET_PROJECTION_APPLY_FAILED",
                message: "Article interaction eligibility projection could not be applied.");
    }

    public static class View
    {
        public static readonly Error IncrementFailed =
            Error.Failure(
                code: "INTERACTION.VIEW_INCREMENT_FAILED",
                message: "The article view contribution could not be recorded.");
    }

    public static class Like
    {
        public static readonly Error AuthenticationRequired =
            Error.Unauthorized(
                code: "INTERACTION.LIKE_AUTHENTICATION_REQUIRED",
                message: "Authentication is required to like or unlike an article.");

        public static readonly Error InvalidUserId =
            Error.Validation(
                code: "INTERACTION.LIKE_INVALID_USER_ID",
                message: "User id must be greater than zero.");

        public static readonly Error StateUnavailable =
            Error.Failure(
                code: "INTERACTION.LIKE_STATE_UNAVAILABLE",
                message: "Article like state is temporarily unavailable.");
    }

    public static class Comment
    {
        public static readonly Error NotFound =
            Error.NotFound(
                code: "INTERACTION.COMMENT_NOT_FOUND",
                message: "Comment was not found.");

        public static readonly Error CommentPublicIdRequired =
            Error.Validation(
                code: "INTERACTION.COMMENT_PUBLIC_ID_REQUIRED",
                message: "Comment public id is required.");

        public static readonly Error ContentRequired =
            Error.Validation(
                code: "INTERACTION.COMMENT_CONTENT_REQUIRED",
                message: "Comment content is required.");

        public static readonly Error ContentTooLong =
            Error.Validation(
                code: "INTERACTION.COMMENT_CONTENT_TOO_LONG",
                message: "Comment content must not exceed 2000 characters.");

        public static readonly Error AuthenticationRequired =
            Error.Unauthorized(
                code: "INTERACTION.COMMENT_AUTHENTICATION_REQUIRED",
                message: "Authentication is required to create or delete a comment.");

        public static readonly Error NotOwner =
            Error.Forbidden(
                code: "INTERACTION.COMMENT_NOT_OWNER",
                message: "You are not allowed to delete this comment.");

        public static readonly Error ReplyNotSupported =
            Error.Validation(
                code: "INTERACTION.COMMENT_REPLY_NOT_SUPPORTED",
                message: "Reply comments are not supported in Interaction V1.");

        public static readonly Error InvalidStatus =
            Error.Validation(
                code: "INTERACTION.COMMENT_INVALID_STATUS",
                message: "Comment status is invalid.");

        public static readonly Error InvalidExpectedVersion =
            Error.Validation(
                code: "INTERACTION.COMMENT_INVALID_EXPECTED_VERSION",
                message: "Expected comment version must be greater than or equal to one.");

        public static readonly Error VersionConflict =
            Error.Conflict(
                code: "INTERACTION.COMMENT_VERSION_CONFLICT",
                message: "The comment has changed. Reload the current state and try again.");

        public static readonly Error InvalidStateTransition =
            Error.Conflict(
                code: "INTERACTION.COMMENT_INVALID_STATE_TRANSITION",
                message: "The requested comment transition is not allowed in its current state.");

        public static readonly Error AlreadyDeleted =
            Error.Conflict(
                code: "INTERACTION.COMMENT_ALREADY_DELETED",
                message: "The comment has already been deleted.");

        public static readonly Error OpenModerationCaseRequiresResolution =
            Error.Conflict(
                code: "INTERACTION.COMMENT_OPEN_MODERATION_CASE_REQUIRES_RESOLUTION",
                message: "This comment has an open moderation case and must be handled through the case-resolution workflow.");

        public static readonly Error ProhibitedContent =
            Error.Validation(
                code: "INTERACTION.COMMENT_PROHIBITED_CONTENT",
                message: "Comment contains prohibited content.");

        public static readonly Error InvalidCommentPublicId =
            Error.Validation(
                code: "INTERACTION.INVALID_COMMENT_PUBLIC_ID",
                message: "Comment public id is invalid.");
    }

    public static class Moderation
    {
        public static readonly Error ReasonCodeRequired =
            Error.Validation(
                code: "INTERACTION.MODERATION_REASON_CODE_REQUIRED",
                message: "Moderation reason code is required.");

        public static readonly Error InvalidReasonCode =
            Error.Validation(
                code: "INTERACTION.MODERATION_INVALID_REASON_CODE",
                message: "Moderation reason code is invalid.");

        public static readonly Error NoteRequiredForOtherReason =
            Error.Validation(
                code: "INTERACTION.MODERATION_NOTE_REQUIRED_FOR_OTHER_REASON",
                message: "Moderation note is required when reason code is Other.");

        public static readonly Error NoteTooLong =
            Error.Validation(
                code: "INTERACTION.MODERATION_NOTE_TOO_LONG",
                message: "Moderation note must not exceed 1000 characters.");

        public static readonly Error InvalidNote =
            Error.Validation(
                code: "INTERACTION.MODERATION_INVALID_NOTE",
                message: "Moderation note must not be blank when provided.");
    }

    public static class CommentReport
    {
        public static readonly Error AuthenticationRequired =
            Error.Unauthorized(
                code: "INTERACTION.COMMENT_REPORT_AUTHENTICATION_REQUIRED",
                message: "Authentication is required to report a comment.");

        public static readonly Error CommentUnavailable =
            Error.NotFound(
                code: "INTERACTION.COMMENT_REPORT_COMMENT_UNAVAILABLE",
                message: "Comment is not available for reporting.");

        public static readonly Error InvalidReporterUserId =
            Error.Validation(
                code: "INTERACTION.COMMENT_REPORT_INVALID_REPORTER_USER_ID",
                message: "Reporter user id must be greater than zero.");

        public static readonly Error ReasonCodeRequired =
            Error.Validation(
                code: "INTERACTION.COMMENT_REPORT_REASON_CODE_REQUIRED",
                message: "Comment report reason code is required.");

        public static readonly Error InvalidReasonCode =
            Error.Validation(
                code: "INTERACTION.COMMENT_REPORT_INVALID_REASON_CODE",
                message: "Comment report reason code is invalid.");

        public static readonly Error InvalidDescription =
            Error.Validation(
                code: "INTERACTION.COMMENT_REPORT_INVALID_DESCRIPTION",
                message: "Comment report description must not be blank when provided.");

        public static readonly Error DescriptionRequiredForOtherReason =
            Error.Validation(
                code: "INTERACTION.COMMENT_REPORT_DESCRIPTION_REQUIRED_FOR_OTHER_REASON",
                message: "Description is required when report reason code is Other.");

        public static readonly Error DescriptionTooLong =
            Error.Validation(
                code: "INTERACTION.COMMENT_REPORT_DESCRIPTION_TOO_LONG",
                message: "Comment report description must not exceed 1000 characters.");

        public static readonly Error CannotReportOwnComment =
            Error.Forbidden(
                code: "INTERACTION.COMMENT_REPORT_CANNOT_REPORT_OWN_COMMENT",
                message: "You cannot report your own comment.");

        public static readonly Error AlreadyReported =
            Error.Conflict(
                code: "INTERACTION.COMMENT_REPORT_ALREADY_EXISTS",
                message: "You have already reported this comment.");

        public static readonly Error CreationFailed =
            Error.Failure(
                code: "INTERACTION.COMMENT_REPORT_CREATION_FAILED",
                message: "The comment report could not be created.");
    }

    public static class CommentModerationCase
    {
        public static readonly Error NotFound =
            Error.NotFound(
                code: "INTERACTION.COMMENT_MODERATION_CASE_NOT_FOUND",
                message: "Comment moderation case was not found.");

        public static readonly Error CasePublicIdRequired =
            Error.Validation(
                code: "INTERACTION.COMMENT_MODERATION_CASE_PUBLIC_ID_REQUIRED",
                message: "Comment moderation case public id is required.");

        public static readonly Error InvalidExpectedVersion =
            Error.Validation(
                code: "INTERACTION.COMMENT_MODERATION_CASE_INVALID_EXPECTED_VERSION",
                message: "Expected moderation case version must be greater than or equal to one.");

        public static readonly Error VersionConflict =
            Error.Conflict(
                code: "INTERACTION.COMMENT_MODERATION_CASE_VERSION_CONFLICT",
                message: "The moderation case has changed. Reload the current state and try again.");

        public static readonly Error NotOpen =
            Error.Conflict(
                code: "INTERACTION.COMMENT_MODERATION_CASE_NOT_OPEN",
                message: "The moderation case is no longer open.");

        public static readonly Error InvalidStateTransition =
            Error.Conflict(
                code: "INTERACTION.COMMENT_MODERATION_CASE_INVALID_STATE_TRANSITION",
                message: "The requested moderation case transition is not allowed in its current state.");

        public static readonly Error AlertAlreadyTriggered =
            Error.Conflict(
                code: "INTERACTION.COMMENT_MODERATION_CASE_ALERT_ALREADY_TRIGGERED",
                message: "An administrator alert has already been triggered for this moderation case.");

        public static readonly Error InvalidCasePublicId =
            Error.Validation(
                code: "INTERACTION.INVALID_COMMENT_MODERATION_CASE_PUBLIC_ID",
                message: "Comment moderation case public id is invalid.");
    }

    public static class Query
    {
        public static readonly Error InvalidPage =
            Error.Validation(
                code: "INTERACTION.INVALID_PAGE",
                message: "Page must be greater than or equal to one.");

        public static readonly Error InvalidPageSize =
            Error.Validation(
                code: "INTERACTION.INVALID_PAGE_SIZE",
                message: "Page size must be greater than zero.");

        public static readonly Error PageSizeTooLarge =
            Error.Validation(
                code: "INTERACTION.PAGE_SIZE_TOO_LARGE",
                message: "Page size must not exceed 200.");

        public static readonly Error InvalidSortDirection =
            Error.Validation(
                code: "INTERACTION.INVALID_SORT_DIRECTION",
                message: "Sort direction must be either ASC or DESC.");

        public static readonly Error InvalidCommentStatus =
            Error.Validation(
                code: "INTERACTION.INVALID_COMMENT_STATUS_FILTER",
                message: "Comment status filter is invalid.");

        public static readonly Error InvalidCaseStatus =
            Error.Validation(
                code: "INTERACTION.INVALID_MODERATION_CASE_STATUS_FILTER",
                message: "Moderation case status filter is invalid.");

        public static readonly Error InvalidCasePriority =
            Error.Validation(
                code: "INTERACTION.INVALID_MODERATION_CASE_PRIORITY_FILTER",
                message: "Moderation case priority filter is invalid.");

        public static readonly Error InvalidAuthorUserId =
            Error.Validation(
                code: "INTERACTION.INVALID_AUTHOR_USER_ID_FILTER",
                message: "Author user id filter must be greater than zero when provided.");
    }

    public static class Counter
    {
        public static readonly Error StatsNotFound =
            Error.NotFound(
                code: "INTERACTION.STATS_NOT_FOUND",
                message: "Interaction statistics were not found for this article.");

        public static readonly Error StatsUnavailable =
            Error.Failure(
                code: "INTERACTION.STATS_UNAVAILABLE",
                message: "Interaction statistics are temporarily unavailable.");

        public static readonly Error MaterializationFailed =
            Error.Failure(
                code: "INTERACTION.STATS_MATERIALIZATION_FAILED",
                message: "Interaction statistics could not be materialized.");

        public static readonly Error InvalidViewStatsMaterializationBatchSize =
            Error.Validation(
                code: "INTERACTION.COUNTER.INVALID_VIEW_STATS_MATERIALIZATION_BATCH_SIZE",
                message: "View stats materialization batch size must be between 1 and 500.");
    }
}