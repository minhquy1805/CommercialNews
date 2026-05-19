using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using Microsoft.Data.SqlClient;

namespace Media.Infrastructure.Persistence.Exceptions;

public sealed class MediaSqlExceptionTranslator : SqlExceptionTranslatorBase
{
    public override Exception Translate(SqlException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.Number switch
        {
            2601 or 2627 => MapUniqueConstraint(exception),
            547 => MapConstraintViolation(exception),
            1205 => new MediaPersistenceException(
                code: "MEDIA.CONCURRENT_MODIFICATION",
                message: "The media operation could not be completed because of a database deadlock. Please retry.",
                innerException: exception),
            -2 => new MediaPersistenceException(
                code: "MEDIA.DEPENDENCY_UNAVAILABLE",
                message: "The media database operation timed out.",
                innerException: exception),
            _ => new MediaPersistenceException(
                code: "MEDIA.PERSISTENCE_ERROR",
                message: "An unexpected SQL persistence error occurred.",
                innerException: exception)
        };
    }

    private static Exception MapUniqueConstraint(SqlException exception)
    {
        string message = exception.Message;

        if (message.Contains("UQ_MediaAsset_PublicId", StringComparison.OrdinalIgnoreCase))
        {
            return new MediaPersistenceException(
                code: "MEDIA.MEDIA_PUBLIC_ID_ALREADY_EXISTS",
                message: "Media public id already exists.",
                innerException: exception);
        }

        if (message.Contains("UQ_ArticleMedia_ArticleId_MediaId", StringComparison.OrdinalIgnoreCase))
        {
            return new MediaPersistenceException(
                code: "MEDIA.ATTACHMENT_ALREADY_EXISTS",
                message: "The media attachment already exists for the article.",
                innerException: exception);
        }

        if (message.Contains("UX_ArticleMedia_ActivePrimaryPerArticle", StringComparison.OrdinalIgnoreCase))
        {
            return new MediaPersistenceException(
                code: "MEDIA.PRIMARY_CONSTRAINT_VIOLATION",
                message: "Only one active primary media is allowed per article.",
                innerException: exception);
        }

        if (message.Contains("UQ_MediaVariant_MediaId_VariantType", StringComparison.OrdinalIgnoreCase))
        {
            return new MediaPersistenceException(
                code: "MEDIA.VARIANT_ALREADY_EXISTS",
                message: "A media variant with the same type already exists for this media asset.",
                innerException: exception);
        }

        return new MediaPersistenceException(
            code: "MEDIA.CONSTRAINT_VIOLATION",
            message: "A media persistence uniqueness constraint was violated.",
            innerException: exception);
    }

    private static Exception MapConstraintViolation(SqlException exception)
    {
        string message = exception.Message;

        if (message.Contains("CK_MediaAsset_MediaType_Allowed", StringComparison.OrdinalIgnoreCase))
        {
            return new MediaPersistenceException(
                code: "MEDIA.TYPE_NOT_ALLOWED",
                message: "Media type is not allowed.",
                innerException: exception);
        }

        if (message.Contains("CK_MediaAsset_FileSizeBytes_NonNegative", StringComparison.OrdinalIgnoreCase))
        {
            return new MediaPersistenceException(
                code: "MEDIA.MEDIA_ASSET_FILE_SIZE_INVALID",
                message: "File size must be greater than or equal to zero.",
                innerException: exception);
        }

        if (message.Contains("CK_MediaAsset_Width_NonNegative", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("CK_MediaAsset_Height_NonNegative", StringComparison.OrdinalIgnoreCase))
        {
            return new MediaPersistenceException(
                code: "MEDIA.MEDIA_ASSET_DIMENSION_INVALID",
                message: "Media dimensions must be greater than or equal to zero.",
                innerException: exception);
        }

        if (message.Contains("CK_MediaAsset_DurationSeconds_NonNegative", StringComparison.OrdinalIgnoreCase))
        {
            return new MediaPersistenceException(
                code: "MEDIA.MEDIA_ASSET_DURATION_INVALID",
                message: "DurationSeconds must be greater than or equal to zero.",
                innerException: exception);
        }

        if (message.Contains("CK_ArticleMedia_SortOrder_NonNegative", StringComparison.OrdinalIgnoreCase))
        {
            return new MediaPersistenceException(
                code: "MEDIA.ARTICLE_MEDIA_SORT_ORDER_INVALID",
                message: "Sort order must be greater than or equal to zero.",
                innerException: exception);
        }

        if (message.Contains("CK_ArticleMedia_Primary_NotDeleted", StringComparison.OrdinalIgnoreCase))
        {
            return new MediaPersistenceException(
                code: "MEDIA.PRIMARY_CONSTRAINT_VIOLATION",
                message: "Deleted article media cannot remain primary.",
                innerException: exception);
        }

        if (message.Contains("FK_ArticleMedia_MediaAsset", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("FK_MediaVariant_MediaAsset", StringComparison.OrdinalIgnoreCase))
        {
            return new MediaPersistenceException(
                code: "MEDIA.MEDIA_NOT_FOUND",
                message: "Media asset was not found.",
                innerException: exception);
        }

        if (message.Contains("FK_ArticleMedia_ArticleMediaSet", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("FK_ArticleMediaSet_Article", StringComparison.OrdinalIgnoreCase))
        {
            return new MediaPersistenceException(
                code: "MEDIA.ARTICLE_NOT_FOUND",
                message: "Article media set or article was not found.",
                innerException: exception);
        }

        if (message.Contains("FK_MediaAsset_CreatedBy_UserAccount", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("FK_MediaAsset_UpdatedBy_UserAccount", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("FK_MediaAsset_DeletedBy_UserAccount", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("FK_MediaAsset_RestoredBy_UserAccount", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("FK_ArticleMediaSet_CreatedBy_UserAccount", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("FK_ArticleMediaSet_UpdatedBy_UserAccount", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("FK_ArticleMedia_CreatedBy_UserAccount", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("FK_ArticleMedia_UpdatedBy_UserAccount", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("FK_ArticleMedia_DeletedBy_UserAccount", StringComparison.OrdinalIgnoreCase))
        {
            return new MediaPersistenceException(
                code: "MEDIA.ACTOR_NOT_FOUND",
                message: "The actor user referenced by the media operation was not found.",
                innerException: exception);
        }

        return new MediaPersistenceException(
            code: "MEDIA.CONSTRAINT_VIOLATION",
            message: "A media persistence constraint was violated.",
            innerException: exception);
    }
}