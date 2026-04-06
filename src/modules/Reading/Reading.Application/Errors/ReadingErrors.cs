using CommercialNews.BuildingBlocks.Results;

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
                code: "READING.ARTICLE_INVALID_ARTICLE_ID",
                message: "Article id must be greater than zero.");

        public static readonly Error SlugRequired =
            Error.Validation(
                code: "READING.SLUG_REQUIRED",
                message: "Slug is required.");

        public static readonly Error ScopeRequired =
            Error.Validation(
                code: "READING.SCOPE_REQUIRED",
                message: "Scope is required.");

        public static readonly Error ScopeTooLong =
            Error.Validation(
                code: "READING.SCOPE_TOO_LONG",
                message: "Scope must not exceed 100 characters.");

        public static readonly Error SlugTooLong =
            Error.Validation(
                code: "READING.SLUG_TOO_LONG",
                message: "Slug must not exceed 200 characters.");
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

        public static readonly Error InvalidSortField =
            Error.Validation(
                code: "READING.INVALID_SORT_FIELD",
                message: "Sort field is invalid.");

        public static readonly Error SearchKeywordRequired =
            Error.Validation(
                code: "READING.SEARCH_KEYWORD_REQUIRED",
                message: "Search keyword is required.");

        public static readonly Error SearchKeywordTooLong =
            Error.Validation(
                code: "READING.SEARCH_KEYWORD_TOO_LONG",
                message: "Search keyword must not exceed 200 characters.");

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
    }

    public static class Visibility
    {
        public static readonly Error PublicAccessDenied =
            Error.NotFound(
                code: "READING.NOT_FOUND",
                message: "Article was not found.");
    }
}