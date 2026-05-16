using CommercialNews.BuildingBlocks.SharedKernel.Results;

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

    public static readonly Error Unexpected =
        Error.Failure(
            code: "SEO.UNEXPECTED_ERROR",
            message: "An unexpected SEO error occurred.");

    public static class Resource
    {
        public static readonly Error InvalidResourceType =
            Error.Validation(
                code: "SEO.INVALID_RESOURCE_TYPE",
                message: "Resource type is required or invalid.");

        public static readonly Error ResourceTypeNotSupported =
            Error.Validation(
                code: "SEO.RESOURCE_TYPE_NOT_SUPPORTED",
                message: "The SEO resource type is not supported.");

        public static readonly Error InvalidResourcePublicId =
            Error.Validation(
                code: "SEO.INVALID_RESOURCE_PUBLIC_ID",
                message: "Resource public id is required or invalid.");

        public static readonly Error ResourceNotFound =
            Error.NotFound(
                code: "SEO.RESOURCE_NOT_FOUND",
                message: "The SEO resource was not found.");
    }

    public static class Article
    {
        public static readonly Error NotFound =
            Error.NotFound(
                code: "SEO.ARTICLE_NOT_FOUND",
                message: "Article was not found.");

        public static readonly Error InvalidArticlePublicId =
            Error.Validation(
                code: "SEO.INVALID_ARTICLE_PUBLIC_ID",
                message: "Article public id is required or invalid.");
    }

    public static class SlugRegistry
    {
        public static readonly Error NotFound =
            Error.NotFound(
                code: "SEO.SLUG_NOT_FOUND",
                message: "Slug route was not found.");

        public static readonly Error RouteNotFound =
            Error.NotFound(
                code: "SEO.ROUTE_NOT_FOUND",
                message: "SEO route was not found.");

        public static readonly Error SafeNotFound =
            Error.NotFound(
                code: "SEO.SAFE_NOT_FOUND",
                message: "The requested route was not found.");

        public static readonly Error Conflict =
            Error.Conflict(
                code: "SEO.SLUG_CONFLICT",
                message: "The slug is already in use for this scope.");

        public static readonly Error RouteOwnershipConflict =
            Error.Conflict(
                code: "SEO.ROUTE_OWNERSHIP_CONFLICT",
                message: "The SEO route is owned by another resource.");

        public static readonly Error ActiveRouteAlreadyExists =
            Error.Conflict(
                code: "SEO.ACTIVE_ROUTE_ALREADY_EXISTS",
                message: "Another active slug already exists for this resource in the same scope.");

        public static readonly Error InvalidSlugId =
            Error.Validation(
                code: "SEO.SLUG_REGISTRY_INVALID_SLUG_ID",
                message: "Slug id must be greater than zero.");

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

        public static readonly Error InvalidResourceType =
            Error.Validation(
                code: "SEO.INVALID_RESOURCE_TYPE",
                message: "Resource type is required or invalid.");

        public static readonly Error InvalidResourcePublicId =
            Error.Validation(
                code: "SEO.INVALID_RESOURCE_PUBLIC_ID",
                message: "Resource public id is required or invalid.");

        public static readonly Error CanonicalUrlTooLong =
            Error.Validation(
                code: "SEO.CANONICAL_URL_TOO_LONG",
                message: "Canonical URL must not exceed 500 characters.");

        public static readonly Error InvalidCanonicalUrl =
            Error.Validation(
                code: "SEO.INVALID_CANONICAL_URL",
                message: "Canonical URL is invalid.");

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

        public static readonly Error VersionMismatch =
            Error.Conflict(
                code: "SEO.VERSION_MISMATCH",
                message: "The slug route was modified by another operation. Please reload and try again.");

        public static readonly Error StaleWriteConflict =
            Error.Conflict(
                code: "SEO.STALE_WRITE_CONFLICT",
                message: "The slug route update is stale and cannot be applied.");

        public static readonly Error RouteDeactivationNotAllowed =
            Error.Validation(
                code: "SEO.ROUTE_DEACTIVATION_NOT_ALLOWED",
                message: "Route deactivation is not allowed by current SEO policy.");
    }

    public static class SeoMetadata
    {
        public static readonly Error NotFound =
            Error.NotFound(
                code: "SEO.METADATA_NOT_FOUND",
                message: "SEO metadata was not found.");

        public static readonly Error AlreadyExists =
            Error.Conflict(
                code: "SEO.METADATA_ALREADY_EXISTS",
                message: "SEO metadata already exists for this resource in this scope.");

        public static readonly Error MetadataOwnershipConflict =
            Error.Conflict(
                code: "SEO.METADATA_OWNERSHIP_CONFLICT",
                message: "The SEO metadata is owned by another resource.");

        public static readonly Error InvalidSeoId =
            Error.Validation(
                code: "SEO.SEO_METADATA_INVALID_SEO_ID",
                message: "Seo id must be greater than zero.");

        public static readonly Error InvalidScope =
            Error.Validation(
                code: "SEO.INVALID_SCOPE",
                message: "Scope is invalid.");

        public static readonly Error InvalidResourceType =
            Error.Validation(
                code: "SEO.INVALID_RESOURCE_TYPE",
                message: "Resource type is required or invalid.");

        public static readonly Error InvalidResourcePublicId =
            Error.Validation(
                code: "SEO.INVALID_RESOURCE_PUBLIC_ID",
                message: "Resource public id is required or invalid.");

        public static readonly Error InvalidVersion =
            Error.Validation(
                code: "SEO.SEO_METADATA_INVALID_VERSION",
                message: "SEO metadata version must be greater than or equal to 1.");

        public static readonly Error InvalidUpdatedAt =
            Error.Validation(
                code: "SEO.SEO_METADATA_INVALID_UPDATED_AT",
                message: "Updated time is invalid for the current SEO metadata state.");

        public static readonly Error CanonicalUrlTooLong =
            Error.Validation(
                code: "SEO.CANONICAL_URL_TOO_LONG",
                message: "Canonical URL must not exceed 500 characters.");

        public static readonly Error InvalidCanonicalUrl =
            Error.Validation(
                code: "SEO.INVALID_CANONICAL_URL",
                message: "Canonical URL is invalid.");

        public static readonly Error CanonicalRuleViolation =
            Error.Validation(
                code: "SEO.CANONICAL_RULE_VIOLATION",
                message: "The canonical URL violates SEO canonical rules.");

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

        public static readonly Error InvalidOgImageUrl =
            Error.Validation(
                code: "SEO.INVALID_OG_IMAGE_URL",
                message: "OG image URL is invalid.");

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

        public static readonly Error RobotsTooLong =
            Error.Validation(
                code: "SEO.ROBOTS_TOO_LONG",
                message: "Robots directive must not exceed 100 characters.");

        public static readonly Error InvalidRobotsDirective =
            Error.Validation(
                code: "SEO.INVALID_ROBOTS_DIRECTIVE",
                message: "Robots directive is invalid.");

        public static readonly Error InvalidMetadataLength =
            Error.Validation(
                code: "SEO.INVALID_METADATA_LENGTH",
                message: "One or more SEO metadata fields exceed allowed length.");

        public static readonly Error VersionMismatch =
            Error.Conflict(
                code: "SEO.VERSION_MISMATCH",
                message: "The SEO metadata was modified by another operation. Please reload and try again.");

        public static readonly Error StaleWriteConflict =
            Error.Conflict(
                code: "SEO.STALE_WRITE_CONFLICT",
                message: "The SEO metadata update is stale and cannot be applied.");

        public static readonly Error ManualOverrideProtected =
            Error.Conflict(
                code: "SEO.MANUAL_OVERRIDE_PROTECTED",
                message: "Manual SEO metadata is protected from automatic sync.");

        public static readonly Error AutoSyncNotAllowed =
            Error.Validation(
                code: "SEO.AUTO_SYNC_NOT_ALLOWED",
                message: "Automatic SEO metadata sync is not allowed by current policy.");
    }

    public static class Sync
    {
        public static readonly Error InvalidSourceAggregateVersion =
            Error.Validation(
                code: "SEO.INVALID_SOURCE_AGGREGATE_VERSION",
                message: "Source aggregate version must be greater than zero.");

        public static readonly Error InvalidLastAppliedMessageId =
            Error.Validation(
                code: "SEO.INVALID_LAST_APPLIED_MESSAGE_ID",
                message: "Last applied message id is required or invalid.");

        public static readonly Error EventDuplicateIgnored =
            Error.Conflict(
                code: "SEO.EVENT_DUPLICATE_IGNORED",
                message: "The SEO event was already applied and was ignored.");

        public static readonly Error EventStaleIgnored =
            Error.Conflict(
                code: "SEO.EVENT_STALE_IGNORED",
                message: "The SEO event was stale and was ignored.");

        public static readonly Error EventVersionGapDetected =
            Error.Conflict(
                code: "SEO.EVENT_VERSION_GAP_DETECTED",
                message: "A version gap was detected while applying SEO sync.");

        public static readonly Error EventResyncRequired =
            Error.Conflict(
                code: "SEO.EVENT_RESYNC_REQUIRED",
                message: "SEO event processing requires truth resync.");

        public static readonly Error ProjectionApplyFailed =
            Error.Failure(
                code: "SEO.PROJECTION_APPLY_FAILED",
                message: "SEO projection apply failed.");

        public static readonly Error RebuildFailed =
            Error.Failure(
                code: "SEO.REBUILD_FAILED",
                message: "SEO rebuild failed.");

        public static readonly Error ReconciliationMismatch =
            Error.Conflict(
                code: "SEO.RECONCILIATION_MISMATCH",
                message: "SEO reconciliation detected a mismatch.");
    }

    public static class Infrastructure
    {
        public static readonly Error StoreUnavailable =
            Error.Failure(
                code: "SEO.STORE_UNAVAILABLE",
                message: "SEO store is unavailable.");

        public static readonly Error CacheUnavailable =
            Error.Failure(
                code: "SEO.CACHE_UNAVAILABLE",
                message: "SEO cache is unavailable.");

        public static readonly Error OutboxWriteFailed =
            Error.Failure(
                code: "SEO.OUTBOX_WRITE_FAILED",
                message: "SEO outbox write failed.");
    }
}