using CommercialNews.BuildingBlocks.Results;

namespace Seo.Application.Errors;

public static class SeoErrors
{
    public static readonly Error ValidationFailed =
        Error.Validation(
            code: "SEO.VALIDATION_FAILED",
            message: "One or more SEO validations failed.");

    public static readonly Error RateLimited =
        Error.RateLimited(
            code: "SEO.RATE_LIMITED",
            message: "Too many SEO requests. Please try again later.");

    public static class SlugRegistry
    {
        public static readonly Error NotFound =
            Error.NotFound(
                code: "SEO.SLUG_NOT_FOUND",
                message: "Slug route was not found.");

        public static readonly Error Conflict =
            Error.Conflict(
                code: "SEO.SLUG_CONFLICT",
                message: "The slug is already in use for this scope.");

        public static readonly Error InvalidSlugId =
            Error.Validation(
                code: "SEO.SLUG_REGISTRY_INVALID_SLUG_ID",
                message: "Slug id must be greater than zero.");

        public static readonly Error InvalidArticleId =
            Error.Validation(
                code: "SEO.SLUG_REGISTRY_INVALID_ARTICLE_ID",
                message: "Article id must be greater than zero.");

        public static readonly Error SlugRequired =
            Error.Validation(
                code: "SEO.INVALID_SLUG",
                message: "Slug is required.");

        public static readonly Error SlugTooLong =
            Error.Validation(
                code: "SEO.SLUG_TOO_LONG",
                message: "Slug must not exceed 200 characters.");

        public static readonly Error InvalidScope =
            Error.Validation(
                code: "SEO.INVALID_SCOPE",
                message: "Scope is invalid.");

        public static readonly Error CanonicalUrlTooLong =
            Error.Validation(
                code: "SEO.CANONICAL_URL_TOO_LONG",
                message: "Canonical URL must not exceed 500 characters.");

        public static readonly Error InvalidVersion =
            Error.Validation(
                code: "SEO.SLUG_REGISTRY_INVALID_VERSION",
                message: "Slug registry version must be greater than or equal to 1.");

        public static readonly Error InvalidUpdatedAt =
            Error.Validation(
                code: "SEO.SLUG_REGISTRY_INVALID_UPDATED_AT",
                message: "Updated time is invalid for the current slug route state.");

        public static readonly Error AlreadyActive =
            Error.Validation(
                code: "SEO.SLUG_REGISTRY_ALREADY_ACTIVE",
                message: "Slug route is already active.");

        public static readonly Error AlreadyInactive =
            Error.Validation(
                code: "SEO.SLUG_REGISTRY_ALREADY_INACTIVE",
                message: "Slug route is already inactive.");

        public static readonly Error Inactive =
            Error.Validation(
                code: "SEO.SLUG_REGISTRY_INACTIVE",
                message: "Slug route is inactive.");

        public static readonly Error ActiveRouteAlreadyExists =
            Error.Conflict(
                code: "SEO.ACTIVE_ROUTE_ALREADY_EXISTS",
                message: "Another active slug already exists for this article in the same scope.");

        public static readonly Error VersionMismatch =
            Error.Conflict(
                code: "SEO.VERSION_MISMATCH",
                message: "The slug route was modified by another operation. Please reload and try again.");

        public static readonly Error StaleWriteConflict =
            Error.Conflict(
                code: "SEO.STALE_WRITE_CONFLICT",
                message: "The slug route update is stale and cannot be applied.");
    }

    public static class SeoMetadata
    {
        public static readonly Error NotFound =
            Error.NotFound(
                code: "SEO.METADATA_NOT_FOUND",
                message: "SEO metadata was not found.");

        public static readonly Error InvalidSeoId =
            Error.Validation(
                code: "SEO.SEO_METADATA_INVALID_SEO_ID",
                message: "Seo id must be greater than zero.");

        public static readonly Error InvalidArticleId =
            Error.Validation(
                code: "SEO.SEO_METADATA_INVALID_ARTICLE_ID",
                message: "Article id must be greater than zero.");

        public static readonly Error AlreadyExists =
            Error.Conflict(
                code: "SEO.METADATA_ALREADY_EXISTS",
                message: "SEO metadata already exists for this article.");

        public static readonly Error InvalidVersion =
            Error.Validation(
                code: "SEO.SEO_METADATA_INVALID_VERSION",
                message: "SEO metadata version must be greater than or equal to 1.");

        public static readonly Error CanonicalUrlTooLong =
            Error.Validation(
                code: "SEO.CANONICAL_URL_TOO_LONG",
                message: "Canonical URL must not exceed 500 characters.");

        public static readonly Error MetaTitleTooLong =
            Error.Validation(
                code: "SEO.META_TITLE_TOO_LONG",
                message: "Meta title must not exceed 300 characters.");

        public static readonly Error MetaDescriptionTooLong =
            Error.Validation(
                code: "SEO.META_DESCRIPTION_TOO_LONG",
                message: "Meta description must not exceed 500 characters.");

        public static readonly Error OgTitleTooLong =
            Error.Validation(
                code: "SEO.OG_TITLE_TOO_LONG",
                message: "OG title must not exceed 300 characters.");

        public static readonly Error OgDescriptionTooLong =
            Error.Validation(
                code: "SEO.OG_DESCRIPTION_TOO_LONG",
                message: "OG description must not exceed 500 characters.");

        public static readonly Error OgImageUrlTooLong =
            Error.Validation(
                code: "SEO.OG_IMAGE_URL_TOO_LONG",
                message: "OG image URL must not exceed 800 characters.");

        public static readonly Error TwitterTitleTooLong =
            Error.Validation(
                code: "SEO.TWITTER_TITLE_TOO_LONG",
                message: "Twitter title must not exceed 300 characters.");

        public static readonly Error TwitterDescriptionTooLong =
            Error.Validation(
                code: "SEO.TWITTER_DESCRIPTION_TOO_LONG",
                message: "Twitter description must not exceed 500 characters.");

        public static readonly Error TwitterImageUrlTooLong =
            Error.Validation(
                code: "SEO.TWITTER_IMAGE_URL_TOO_LONG",
                message: "Twitter image URL must not exceed 800 characters.");

        public static readonly Error VersionMismatch =
            Error.Conflict(
                code: "SEO.VERSION_MISMATCH",
                message: "The SEO metadata was modified by another operation. Please reload and try again.");

        public static readonly Error StaleWriteConflict =
            Error.Conflict(
                code: "SEO.STALE_WRITE_CONFLICT",
                message: "The SEO metadata update is stale and cannot be applied.");

        public static readonly Error CanonicalRuleViolation =
            Error.Validation(
                code: "SEO.CANONICAL_RULE_VIOLATION",
                message: "The canonical URL violates SEO canonical rules.");
    }

    public static class Article
    {
        public static readonly Error NotFound =
            Error.NotFound(
                code: "SEO.ARTICLE_NOT_FOUND",
                message: "Article was not found.");

        public static readonly Error InvalidArticleId =
            Error.Validation(
                code: "SEO.ARTICLE_INVALID_ARTICLE_ID",
                message: "Article id must be greater than zero.");
    }

    public static class Resource
    {
        public static readonly Error ResourceTypeNotSupported =
            Error.Validation(
                code: "SEO.RESOURCE_TYPE_NOT_SUPPORTED",
                message: "The SEO resource type is not supported.");

        public static readonly Error InvalidResourceId =
            Error.Validation(
                code: "SEO.INVALID_RESOURCE_ID",
                message: "Resource id must be greater than zero.");
    }
}