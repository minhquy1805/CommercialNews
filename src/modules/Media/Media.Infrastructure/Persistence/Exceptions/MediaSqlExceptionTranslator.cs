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

            _ => new MediaPersistenceException(
                code: "MEDIA.VALIDATION_FAILED",
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
                code: "MEDIA.MEDIA_PUBLIC_ID_INVALID",
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
                code: "MEDIA.VARIANT_TYPE_REQUIRED",
                message: "A media variant with the same type already exists for this media asset.",
                innerException: exception);
        }

        return new MediaPersistenceException(
            code: "MEDIA.VALIDATION_FAILED",
            message: "A persistence constraint was violated.",
            innerException: exception);
    }
}