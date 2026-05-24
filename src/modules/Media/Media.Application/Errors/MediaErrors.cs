using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Media.Application.Errors;

public static class MediaErrors
{
    public static readonly Error ValidationFailed =
        Error.Validation(
            code: "MEDIA.VALIDATION_FAILED",
            message: "One or more media validations failed.");

    public static readonly Error InvalidState =
        Error.Validation(
            code: "MEDIA.INVALID_STATE",
            message: "The media operation is not valid for the current state.");

    public static readonly Error VersionConflict =
        Error.Conflict(
            code: "MEDIA.VERSION_CONFLICT",
            message: "The media resource version does not match the expected version.");

    public static readonly Error ExpectedVersionRequired =
        Error.Validation(
            code: "MEDIA.EXPECTED_VERSION_REQUIRED",
            message: "Expected version is required for this operation.");

    public static readonly Error ConstraintViolation =
        Error.Conflict(
            code: "MEDIA.CONSTRAINT_VIOLATION",
            message: "A media persistence constraint was violated.");

    public static readonly Error PersistenceError =
        Error.Failure(
            code: "MEDIA.PERSISTENCE_ERROR",
            message: "An unexpected media persistence error occurred.");

    public static readonly Error DependencyUnavailable =
        Error.Failure(
            code: "MEDIA.DEPENDENCY_UNAVAILABLE",
            message: "A media dependency is temporarily unavailable.");

    public static readonly Error ConcurrentModification =
        Error.Conflict(
            code: "MEDIA.CONCURRENT_MODIFICATION",
            message: "The media operation could not be completed because the resource was modified concurrently.");

    public static readonly Error RateLimited =
        Error.RateLimited(
            code: "MEDIA.RATE_LIMITED",
            message: "Too many media requests. Please try again later.");

    public static class MediaAsset
    {
        public static readonly Error NotFound =
            Error.NotFound(
                code: "MEDIA.MEDIA_NOT_FOUND",
                message: "Media asset was not found.");

        public static readonly Error Deleted =
            Error.Conflict(
                code: "MEDIA.MEDIA_DELETED",
                message: "Media asset is deleted.");

        public static readonly Error AlreadyDeleted =
            Error.Validation(
                code: "MEDIA.MEDIA_ALREADY_DELETED",
                message: "Media asset is already deleted.");

        public static readonly Error NotDeleted =
            Error.Validation(
                code: "MEDIA.MEDIA_NOT_DELETED",
                message: "Media asset is not deleted.");

        public static readonly Error PublicIdRequired =
            Error.Validation(
                code: "MEDIA.MEDIA_PUBLIC_ID_REQUIRED",
                message: "Media public id is required.");

        public static readonly Error PublicIdInvalid =
            Error.Validation(
                code: "MEDIA.MEDIA_PUBLIC_ID_INVALID",
                message: "Media public id must be exactly 26 characters.");

        public static readonly Error PublicIdAlreadyExists =
            Error.Conflict(
                code: "MEDIA.MEDIA_PUBLIC_ID_ALREADY_EXISTS",
                message: "Media public id already exists.");

        public static readonly Error StorageProviderRequired =
            Error.Validation(
                code: "MEDIA.MEDIA_STORAGE_PROVIDER_REQUIRED",
                message: "Storage provider is required.");

        public static readonly Error StorageProviderTooLong =
            Error.Validation(
                code: "MEDIA.MEDIA_STORAGE_PROVIDER_TOO_LONG",
                message: "Storage provider must not exceed 30 characters.");

        public static readonly Error UrlRequired =
            Error.Validation(
                code: "MEDIA.MEDIA_URL_REQUIRED",
                message: "Media URL is required.");

        public static readonly Error UrlTooLong =
            Error.Validation(
                code: "MEDIA.MEDIA_URL_TOO_LONG",
                message: "Media URL must not exceed 800 characters.");

        public static readonly Error StoragePathTooLong =
            Error.Validation(
                code: "MEDIA.MEDIA_STORAGE_PATH_TOO_LONG",
                message: "Storage path must not exceed 800 characters.");

        public static readonly Error FileNameTooLong =
            Error.Validation(
                code: "MEDIA.MEDIA_FILE_NAME_TOO_LONG",
                message: "File name must not exceed 255 characters.");

        public static readonly Error TypeNotAllowed =
            Error.Validation(
                code: "MEDIA.TYPE_NOT_ALLOWED",
                message: "Media type is not allowed.");

        public static readonly Error MimeTypeTooLong =
            Error.Validation(
                code: "MEDIA.MEDIA_MIME_TYPE_TOO_LONG",
                message: "Mime type must not exceed 100 characters.");

        public static readonly Error FileSizeInvalid =
            Error.Validation(
                code: "MEDIA.MEDIA_FILE_SIZE_INVALID",
                message: "File size must be greater than or equal to zero.");

        public static readonly Error DimensionInvalid =
            Error.Validation(
                code: "MEDIA.MEDIA_DIMENSION_INVALID",
                message: "Media dimensions must be greater than or equal to zero.");

        public static readonly Error WidthInvalid =
            Error.Validation(
                code: "MEDIA.MEDIA_WIDTH_INVALID",
                message: "Width must be greater than or equal to zero.");

        public static readonly Error HeightInvalid =
            Error.Validation(
                code: "MEDIA.MEDIA_HEIGHT_INVALID",
                message: "Height must be greater than or equal to zero.");

        public static readonly Error DurationInvalid =
            Error.Validation(
                code: "MEDIA.MEDIA_DURATION_INVALID",
                message: "Duration must be greater than or equal to zero.");

        public static readonly Error AltTextTooLong =
            Error.Validation(
                code: "MEDIA.MEDIA_ALT_TEXT_TOO_LONG",
                message: "Alt text must not exceed 300 characters.");

        public static readonly Error InvalidMediaId =
            Error.Validation(
                code: "MEDIA.MEDIA_INVALID_MEDIA_ID",
                message: "Media id must be greater than zero.");

        public static readonly Error InvalidVersion =
            Error.Validation(
                code: "MEDIA.MEDIA_INVALID_VERSION",
                message: "Media version must be greater than or equal to 1.");

        public static readonly Error InvalidDeletedAt =
            Error.Validation(
                code: "MEDIA.MEDIA_INVALID_DELETED_AT",
                message: "Deleted time is invalid for the current media state.");

        public static readonly Error InvalidDeletedBy =
            Error.Validation(
                code: "MEDIA.MEDIA_INVALID_DELETED_BY",
                message: "Deleted-by user is invalid for the current media state.");

        public static readonly Error RestoreWindowExpired =
            Error.Conflict(
                code: "MEDIA.RESTORE_WINDOW_EXPIRED",
                message: "The restore window has expired.");

        public static readonly Error RestoreUntilInvalid =
            Error.Validation(
                code: "MEDIA.MEDIA_RESTORE_UNTIL_INVALID",
                message: "Restore-until time must be greater than or equal to the current time.");

        public static readonly Error RestoredAtRequired =
            Error.Validation(
                code: "MEDIA.MEDIA_RESTORED_AT_REQUIRED",
                message: "Restored media asset must have RestoredAt when RestoredBy exists.");
    }

    public static class ArticleMediaSet
    {
        public static readonly Error NotFound =
            Error.NotFound(
                code: "MEDIA.ARTICLE_MEDIA_SET_NOT_FOUND",
                message: "Article media set was not found.");

        public static readonly Error InvalidArticleId =
            Error.Validation(
                code: "MEDIA.ARTICLE_MEDIA_SET_INVALID_ARTICLE_ID",
                message: "Article id must be greater than zero.");

        public static readonly Error InvalidVersion =
            Error.Validation(
                code: "MEDIA.ARTICLE_MEDIA_SET_INVALID_VERSION",
                message: "Article media set version must be greater than or equal to zero.");

        public static readonly Error VersionConflict =
            Error.Conflict(
                code: "MEDIA.VERSION_CONFLICT",
                message: "Article media set version does not match the expected version.");

        public static readonly Error ExpectedVersionRequired =
            Error.Validation(
                code: "MEDIA.EXPECTED_VERSION_REQUIRED",
                message: "Expected version is required.");
    }

    public static class ArticleMedia
    {
        public static readonly Error NotFound =
            Error.NotFound(
                code: "MEDIA.ATTACHMENT_NOT_FOUND",
                message: "Article media attachment was not found.");

        public static readonly Error AlreadyExists =
            Error.Conflict(
                code: "MEDIA.ATTACHMENT_ALREADY_EXISTS",
                message: "The media attachment already exists for the article.");

        public static readonly Error AlreadyDeleted =
            Error.Validation(
                code: "MEDIA.ATTACHMENT_ALREADY_DELETED",
                message: "The media attachment is already deleted.");

        public static readonly Error NotDeleted =
            Error.Validation(
                code: "MEDIA.ATTACHMENT_NOT_DELETED",
                message: "The media attachment is not deleted.");

        public static readonly Error MediaNotAttached =
            Error.NotFound(
                code: "MEDIA.MEDIA_NOT_ATTACHED",
                message: "The media asset is not attached to the article.");

        public static readonly Error InvalidArticleMediaId =
            Error.Validation(
                code: "MEDIA.ATTACHMENT_INVALID_ARTICLE_MEDIA_ID",
                message: "Article media id must be greater than zero.");

        public static readonly Error InvalidArticleId =
            Error.Validation(
                code: "MEDIA.ATTACHMENT_INVALID_ARTICLE_ID",
                message: "Article id must be greater than zero.");

        public static readonly Error InvalidMediaId =
            Error.Validation(
                code: "MEDIA.ATTACHMENT_INVALID_MEDIA_ID",
                message: "Media id must be greater than zero.");

        public static readonly Error InvalidSortOrder =
            Error.Validation(
                code: "MEDIA.ATTACHMENT_SORT_ORDER_INVALID",
                message: "Sort order must be greater than or equal to zero.");

        public static readonly Error InvalidVersion =
            Error.Validation(
                code: "MEDIA.ATTACHMENT_INVALID_VERSION",
                message: "Article media version must be greater than or equal to 1.");

        public static readonly Error InvalidDeletedAt =
            Error.Validation(
                code: "MEDIA.ATTACHMENT_INVALID_DELETED_AT",
                message: "Deleted time is invalid for the current attachment state.");

        public static readonly Error InvalidDeletedBy =
            Error.Validation(
                code: "MEDIA.ATTACHMENT_INVALID_DELETED_BY",
                message: "Deleted-by user is invalid for the current attachment state.");

        public static readonly Error PrimaryConstraintViolation =
            Error.Conflict(
                code: "MEDIA.PRIMARY_CONSTRAINT_VIOLATION",
                message: "Only one active primary media is allowed per article.");

        public static readonly Error PrimaryMustBeImage =
            Error.Validation(
                code: "MEDIA.ARTICLE_PRIMARY_MEDIA_MUST_BE_IMAGE",
                message: "Article primary media must be an image.");

        public static readonly Error AltTextOverrideTooLong =
            Error.Validation(
                code: "MEDIA.ATTACHMENT_ALT_TEXT_OVERRIDE_TOO_LONG",
                message: "Attachment alt text override must not exceed 300 characters.");

        public static readonly Error CaptionTooLong =
            Error.Validation(
                code: "MEDIA.ATTACHMENT_CAPTION_TOO_LONG",
                message: "Attachment caption must not exceed 300 characters.");

        public static readonly Error InvalidReorderList =
            Error.Validation(
                code: "MEDIA.INVALID_REORDER_LIST",
                message: "The reorder list is invalid.");

        public static readonly Error ExpectedVersionRequired =
            Error.Validation(
                code: "MEDIA.EXPECTED_VERSION_REQUIRED",
                message: "Expected version is required.");

        public static readonly Error VersionConflict =
            Error.Conflict(
                code: "MEDIA.VERSION_CONFLICT",
                message: "Article media set version does not match the expected version.");
    }

    public static class Variant
    {
        public static readonly Error NotFound =
            Error.NotFound(
                code: "MEDIA.VARIANT_NOT_FOUND",
                message: "Media variant was not found.");

        public static readonly Error AlreadyExists =
            Error.Conflict(
                code: "MEDIA.VARIANT_ALREADY_EXISTS",
                message: "A media variant with the same type already exists for this media asset.");

        public static readonly Error InvalidVariantId =
            Error.Validation(
                code: "MEDIA.VARIANT_INVALID_VARIANT_ID",
                message: "Variant id must be greater than zero.");

        public static readonly Error InvalidMediaId =
            Error.Validation(
                code: "MEDIA.VARIANT_INVALID_MEDIA_ID",
                message: "Media id must be greater than zero.");

        public static readonly Error VariantTypeRequired =
            Error.Validation(
                code: "MEDIA.VARIANT_TYPE_REQUIRED",
                message: "Variant type is required.");

        public static readonly Error VariantTypeTooLong =
            Error.Validation(
                code: "MEDIA.VARIANT_TYPE_TOO_LONG",
                message: "Variant type must not exceed 30 characters.");

        public static readonly Error UrlRequired =
            Error.Validation(
                code: "MEDIA.VARIANT_URL_REQUIRED",
                message: "Variant URL is required.");

        public static readonly Error UrlTooLong =
            Error.Validation(
                code: "MEDIA.VARIANT_URL_TOO_LONG",
                message: "Variant URL must not exceed 800 characters.");

        public static readonly Error WidthInvalid =
            Error.Validation(
                code: "MEDIA.VARIANT_WIDTH_INVALID",
                message: "Variant width must be greater than or equal to zero.");

        public static readonly Error HeightInvalid =
            Error.Validation(
                code: "MEDIA.VARIANT_HEIGHT_INVALID",
                message: "Variant height must be greater than or equal to zero.");

        public static readonly Error FileSizeInvalid =
            Error.Validation(
                code: "MEDIA.VARIANT_FILE_SIZE_INVALID",
                message: "Variant file size must be greater than or equal to zero.");
    }

    public static class Article
    {
        public static readonly Error NotFound =
            Error.NotFound(
                code: "MEDIA.ARTICLE_NOT_FOUND",
                message: "Article was not found.");
    }

    public static class Actor
    {
        public static readonly Error NotFound =
            Error.NotFound(
                code: "MEDIA.ACTOR_NOT_FOUND",
                message: "The actor user referenced by the media operation was not found.");
    }
}
