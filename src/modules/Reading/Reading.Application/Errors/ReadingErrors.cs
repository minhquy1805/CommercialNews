using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Reading.Application.Errors;

public static class ReadingErrors
{
    public static readonly Error ValidationFailed =
        Error.Validation(
            code: "READING.VALIDATION_FAILED",
            message: "One or more reading validations failed.");

    public static readonly Error RateLimited =
        Error.RateLimited(
            code: "READING.RATE_LIMITED",
            message: "Too many reading requests. Please try again later.");

    public static readonly Error ServiceUnavailable =
        Error.Failure(
            code: "READING.SERVICE_UNAVAILABLE",
            message: "Reading service is temporarily unavailable.");

    public static readonly Error InternalError =
        Error.Failure(
            code: "READING.INTERNAL_ERROR",
            message: "An unexpected reading error occurred.");

    public static readonly Error DependencyDegraded =
        Error.Failure(
            code: "READING.DEPENDENCY_DEGRADED",
            message: "One or more optional reading enrichments are degraded.");

    public static class Article
    {
        public static readonly Error NotFound =
            Error.NotFound(
                code: "READING.NOT_FOUND",
                message: "Article was not found.");

        public static readonly Error InvalidArticleId =
            Error.Validation(
                code: "READING.INVALID_ARTICLE_ID",
                message: "Article id must be greater than zero.");

        public static readonly Error InvalidArticlePublicId =
            Error.Validation(
                code: "READING.INVALID_ARTICLE_PUBLIC_ID",
                message: "Article public id must be a valid 26-character value.");
    }

    public static class Route
    {
        public static readonly Error SlugRequired =
            Error.Validation(
                code: "READING.SLUG_REQUIRED",
                message: "Slug is required.");

        public static readonly Error SlugTooLong =
            Error.Validation(
                code: "READING.SLUG_TOO_LONG",
                message: "Slug must not exceed 200 characters.");

        public static readonly Error RouteNotResolved =
            Error.NotFound(
                code: "READING.NOT_FOUND",
                message: "Article was not found.");

        public static readonly Error RouteInactive =
            Error.NotFound(
                code: "READING.NOT_FOUND",
                message: "Article was not found.");

        public static readonly Error RouteResourceTypeInvalid =
            Error.NotFound(
                code: "READING.NOT_FOUND",
                message: "Article was not found.");

        public static readonly Error RouteResolutionUnavailable =
            Error.Failure(
                code: "READING.ROUTE_RESOLUTION_UNAVAILABLE",
                message: "Article route resolution is temporarily unavailable.");
    }

    public static class Query
    {
        public static readonly Error InvalidPage =
            Error.Validation(
                code: "READING.INVALID_PAGE",
                message: "Page must be greater than or equal to 1.");

        public static readonly Error InvalidPageSize =
            Error.Validation(
                code: "READING.INVALID_PAGE_SIZE",
                message: "Page size must be greater than zero.");

        public static readonly Error PageSizeTooLarge =
            Error.Validation(
                code: "READING.PAGE_SIZE_TOO_LARGE",
                message: "Page size exceeds the maximum allowed value.");

        public static readonly Error InvalidSort =
            Error.Validation(
                code: "READING.INVALID_SORT",
                message: "Sort value is invalid.");

        public static readonly Error InvalidCategoryId =
            Error.Validation(
                code: "READING.INVALID_CATEGORY_ID",
                message: "Category id must be greater than zero.");

        public static readonly Error InvalidTagId =
            Error.Validation(
                code: "READING.INVALID_TAG_ID",
                message: "Tag id must be greater than zero.");

        public static readonly Error InvalidLimit =
            Error.Validation(
                code: "READING.INVALID_LIMIT",
                message: "Limit must be greater than zero.");

        public static readonly Error LimitTooLarge =
            Error.Validation(
                code: "READING.LIMIT_TOO_LARGE",
                message: "Limit exceeds the maximum allowed value.");

        public static readonly Error SearchQueryRequired =
            Error.Validation(
                code: "READING.SEARCH_QUERY_REQUIRED",
                message: "Search query is required.");

        public static readonly Error SearchQueryTooLong =
            Error.Validation(
                code: "READING.SEARCH_QUERY_TOO_LONG",
                message: "Search query must not exceed 300 characters.");
    }

    public static class Projection
    {
        public static readonly Error InvalidSourceStatus =
            Error.Validation(
                code: "READING.INVALID_SOURCE_STATUS",
                message: "Source article status is invalid.");

        public static readonly Error InvalidSourceVersion =
            Error.Validation(
                code: "READING.INVALID_SOURCE_VERSION",
                message: "Source version must be greater than zero.");

        public static readonly Error InvalidMessageId =
            Error.Validation(
                code: "READING.INVALID_MESSAGE_ID",
                message: "Message id must be a valid 26-character value.");

        public static readonly Error ProjectionUnavailable =
            Error.Failure(
                code: "READING.PROJECTION_UNAVAILABLE",
                message: "Reading projection is temporarily unavailable.");

        public static readonly Error ProjectionStale =
            Error.Failure(
                code: "READING.PROJECTION_STALE",
                message: "Reading projection is stale.");

        public static readonly Error ProjectionApplyFailed =
            Error.Failure(
                code: "READING.PROJECTION_APPLY_FAILED",
                message: "Reading projection apply failed.");

        public static readonly Error StaleProjectionEvent =
            Error.Conflict(
                code: "READING.STALE_PROJECTION_EVENT",
                message: "Projection event was ignored because it is stale.");

        public static readonly Error DuplicateProjectionEvent =
            Error.Conflict(
                code: "READING.DUPLICATE_PROJECTION_EVENT",
                message: "Projection event was ignored because it was already applied.");

        public static readonly Error MissingProjection =
            Error.NotFound(
                code: "READING.PROJECTION_NOT_FOUND",
                message: "Reading projection was not found.");
    }

    public static class Visibility
    {
        public static readonly Error PublicAccessDenied =
            Error.NotFound(
                code: "READING.NOT_FOUND",
                message: "Article was not found.");

        public static readonly Error ArticleNotPublic =
            Error.NotFound(
                code: "READING.NOT_FOUND",
                message: "Article was not found.");

        public static readonly Error VisibilityUncertain =
            Error.NotFound(
                code: "READING.NOT_FOUND",
                message: "Article was not found.");
    }

    public static class Related
    {
        public static readonly Error ParentArticleNotFound =
            Error.NotFound(
                code: "READING.NOT_FOUND",
                message: "Article was not found.");

        public static readonly Error RelatedUnavailable =
            Error.Failure(
                code: "READING.RELATED_UNAVAILABLE",
                message: "Related articles are temporarily unavailable.");
    }
}
