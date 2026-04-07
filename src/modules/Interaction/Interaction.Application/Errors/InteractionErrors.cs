using CommercialNews.BuildingBlocks.Results;

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

    public static readonly Error DependencyDegraded =
        Error.Failure(
            code: "INTERACTION.DEPENDENCY_DEGRADED",
            message: "One or more optional interaction enrichments are degraded.");

    public static class Article
    {
        public static readonly Error NotFound =
            Error.NotFound(
                code: "INTERACTION.ARTICLE_NOT_FOUND",
                message: "Article was not found.");

        public static readonly Error InvalidArticleId =
            Error.Validation(
                code: "INTERACTION.ARTICLE_INVALID_ARTICLE_ID",
                message: "Article id must be greater than zero.");

        public static readonly Error DisabledForArticleState =
            Error.Validation(
                code: "INTERACTION.DISABLED_FOR_ARTICLE_STATE",
                message: "Interaction is disabled for the current article state.");
    }

    public static class View
    {
        public static readonly Error VisitorKeyTooLong =
            Error.Validation(
                code: "INTERACTION.VIEW_VISITOR_KEY_TOO_LONG",
                message: "Visitor key must not exceed 100 characters.");

        public static readonly Error IpAddressTooLong =
            Error.Validation(
                code: "INTERACTION.VIEW_IP_ADDRESS_TOO_LONG",
                message: "IP address must not exceed 64 characters.");

        public static readonly Error UserAgentTooLong =
            Error.Validation(
                code: "INTERACTION.VIEW_USER_AGENT_TOO_LONG",
                message: "User agent must not exceed 512 characters.");
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
    }

    public static class Comment
    {
        public static readonly Error NotFound =
            Error.NotFound(
                code: "INTERACTION.COMMENT_NOT_FOUND",
                message: "Comment was not found.");

        public static readonly Error InvalidCommentId =
            Error.Validation(
                code: "INTERACTION.COMMENT_INVALID_COMMENT_ID",
                message: "Comment id must be greater than zero.");

        public static readonly Error InvalidParentCommentId =
            Error.Validation(
                code: "INTERACTION.COMMENT_INVALID_PARENT_COMMENT_ID",
                message: "Parent comment id must be greater than zero.");

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
                message: "Authentication is required to create, update, or delete a comment.");

        public static readonly Error NotOwner =
            Error.Forbidden(
                code: "INTERACTION.NOT_COMMENT_OWNER",
                message: "You are not allowed to modify this comment.");

        public static readonly Error AlreadyDeleted =
            Error.Validation(
                code: "INTERACTION.COMMENT_ALREADY_DELETED",
                message: "Deleted comment cannot be modified.");

        public static readonly Error InvalidStatus =
            Error.Validation(
                code: "INTERACTION.COMMENT_INVALID_STATUS",
                message: "Comment status is invalid.");
    }

    public static class Query
    {
        public static readonly Error InvalidPage =
            Error.Validation(
                code: "INTERACTION.INVALID_PAGE",
                message: "Page must be greater than or equal to 1.");

        public static readonly Error InvalidPageSize =
            Error.Validation(
                code: "INTERACTION.INVALID_PAGE_SIZE",
                message: "Page size must be greater than zero.");

        public static readonly Error PageSizeTooLarge =
            Error.Validation(
                code: "INTERACTION.PAGE_SIZE_TOO_LARGE",
                message: "Page size exceeds the maximum allowed value.");

        public static readonly Error InvalidSortField =
            Error.Validation(
                code: "INTERACTION.INVALID_SORT_FIELD",
                message: "Sort field is invalid.");

        public static readonly Error InvalidLimit =
            Error.Validation(
                code: "INTERACTION.INVALID_LIMIT",
                message: "Limit must be greater than zero.");

        public static readonly Error LimitTooLarge =
            Error.Validation(
                code: "INTERACTION.LIMIT_TOO_LARGE",
                message: "Limit exceeds the maximum allowed value.");
    }

    public static class Counter
    {
        public static readonly Error StatsUnavailable =
            Error.Failure(
                code: "INTERACTION.STATS_UNAVAILABLE",
                message: "Interaction statistics are temporarily unavailable.");
    }
}