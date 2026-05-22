using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using Microsoft.Data.SqlClient;

namespace Reading.Infrastructure.Persistence.Exceptions;

public sealed class ReadingSqlExceptionTranslator : SqlExceptionTranslatorBase
{
    public override Exception Translate(SqlException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.Number switch
        {
            58201 => Reading(
                "READING.DATABASE_NOT_FOUND",
                "Database CommercialNews does not exist.",
                exception),

            58202 => Reading(
                "READING.SCHEMA_NOT_FOUND",
                "Schema reading does not exist.",
                exception),

            58203 or 58204 or 58205 => Reading(
                "READING.PROJECTION_STORE_NOT_FOUND",
                "A required Reading projection table does not exist.",
                exception),

            58210 or 58240 or 58251 => Reading(
                "READING.INVALID_ARTICLE_PUBLIC_ID",
                "Article public id must be a valid 26-character public id.",
                exception),

            58220 => Reading(
                "READING.INVALID_SLUG",
                "Slug is required.",
                exception),

            58230 => Reading(
                "READING.INVALID_SEARCH_QUERY",
                "Keyword is required.",
                exception),

            58250 or 58260 or 58270 or 58280 or 58300 or 58320 or 58340 or 58360 => Reading(
                "READING.INVALID_ARTICLE_ID",
                "Article id must be greater than zero.",
                exception),

            58301 or 58321 or 58361 => Reading(
                "READING.INVALID_MEDIA_ID",
                "Media id must be greater than zero.",
                exception),

            58302 or 58322 => Reading(
                "READING.INVALID_MEDIA_PUBLIC_ID",
                "Media public id must be a valid 26-character public id.",
                exception),

            58303 or 58323 => Reading(
                "READING.INVALID_MEDIA_URL",
                "Media URL is required.",
                exception),

            58304 or 58324 => Reading(
                "READING.INVALID_MEDIA_TYPE",
                "Media type is required.",
                exception),

            58305 or 58325 or 58343 or 58344 => Reading(
                "READING.INVALID_MEDIA_SORT_ORDER",
                "Media sort order is invalid.",
                exception),

            58252 => Reading(
                "READING.INVALID_TITLE",
                "Article title is required.",
                exception),

            58253 => Reading(
                "READING.INVALID_SUMMARY",
                "Article summary is required.",
                exception),

            58254 => Reading(
                "READING.INVALID_BODY",
                "Article body is required.",
                exception),

            58255 or 58261 => Reading(
                "READING.INVALID_SOURCE_STATUS",
                "Source article status is invalid.",
                exception),

            58256 or 58262 or 58306 or 58326 or 58341 or 58362 => Reading(
                "READING.INVALID_SOURCE_VERSION",
                "Source version must be greater than zero.",
                exception),

            58342 => Reading(
                "READING.MEDIA_REORDER_ITEMS_REQUIRED",
                "Media reorder items are required.",
                exception),

            58281 => Reading(
                "READING.INVALID_COUNTER_VALUE",
                "Reading counters must not be negative.",
                exception),

            2601 or 2627 => MapUniqueConstraint(exception),
            547 => MapForeignKeyOrCheckConstraint(exception),

            _ => Reading(
                "READING.PERSISTENCE_ERROR",
                "An unexpected SQL persistence error occurred.",
                exception)
        };
    }

    private static Exception MapUniqueConstraint(SqlException exception)
    {
        string message = exception.Message;

        if (message.Contains(
                "PK_ArticleReadModel",
                StringComparison.OrdinalIgnoreCase))
        {
            return new ReadingPersistenceException(
                code: "READING.ARTICLE_PROJECTION_ALREADY_EXISTS",
                message: "Article read model already exists.",
                innerException: exception);
        }

        if (message.Contains(
                "UQ_ArticleReadModel_ArticlePublicId",
                StringComparison.OrdinalIgnoreCase))
        {
            return new ReadingPersistenceException(
                code: "READING.ARTICLE_PUBLIC_ID_ALREADY_EXISTS",
                message: "Article public id already exists in the reading projection.",
                innerException: exception);
        }

        if (ContainsAny(
                message,
                "IX_ArticleReadModel_Public_Slug",
                "UX_ArticleReadModel_Slug_Public"))
        {
            return new ReadingPersistenceException(
                code: "READING.SLUG_ALREADY_PROJECTED",
                message: "Slug is already projected for another public article.",
                innerException: exception);
        }

        if (message.Contains(
                "PK_ArticleReadModelTag",
                StringComparison.OrdinalIgnoreCase))
        {
            return new ReadingPersistenceException(
                code: "READING.ARTICLE_TAG_PROJECTION_ALREADY_EXISTS",
                message: "Article tag projection already exists.",
                innerException: exception);
        }

        if (message.Contains(
                "PK_ArticleReadModelMedia",
                StringComparison.OrdinalIgnoreCase))
        {
            return new ReadingPersistenceException(
                code: "READING.ARTICLE_MEDIA_PROJECTION_ALREADY_EXISTS",
                message: "Article media projection already exists.",
                innerException: exception);
        }

        return Reading(
            "READING.UNIQUE_CONSTRAINT_VIOLATED",
            "A persistence uniqueness constraint was violated.",
            exception);
    }

    private static Exception MapForeignKeyOrCheckConstraint(SqlException exception)
    {
        string message = exception.Message;

        if (message.Contains(
                "FK_ArticleReadModelTag_ArticleReadModel",
                StringComparison.OrdinalIgnoreCase) ||
            message.Contains(
                "FK_ArticleReadModelMedia_ArticleReadModel",
                StringComparison.OrdinalIgnoreCase))
        {
            return new ReadingPersistenceException(
                code: "READING.ARTICLE_PROJECTION_NOT_FOUND",
                message: "The referenced article read model does not exist.",
                innerException: exception);
        }

        if (ContainsAny(
                message,
                "CK_ArticleReadModel_ArticlePublicId_Length",
                "CK_ArticleReadModel_ArticlePublicId_NotBlank"))
        {
            return new ReadingPersistenceException(
                code: "READING.INVALID_ARTICLE_PUBLIC_ID",
                message: "Article public id is required.",
                innerException: exception);
        }

        if (message.Contains(
                "CK_ArticleReadModel_Title_NotBlank",
                StringComparison.OrdinalIgnoreCase))
        {
            return new ReadingPersistenceException(
                code: "READING.INVALID_TITLE",
                message: "Article title is required.",
                innerException: exception);
        }

        if (message.Contains(
                "CK_ArticleReadModel_Summary_NotBlank",
                StringComparison.OrdinalIgnoreCase))
        {
            return new ReadingPersistenceException(
                code: "READING.INVALID_SUMMARY",
                message: "Article summary is required.",
                innerException: exception);
        }

        if (message.Contains(
                "CK_ArticleReadModel_Body_NotBlank",
                StringComparison.OrdinalIgnoreCase))
        {
            return new ReadingPersistenceException(
                code: "READING.INVALID_BODY",
                message: "Article body is required.",
                innerException: exception);
        }

        if (message.Contains(
                "CK_ArticleReadModel_Status",
                StringComparison.OrdinalIgnoreCase))
        {
            return new ReadingPersistenceException(
                code: "READING.INVALID_SOURCE_STATUS",
                message: "Source article status is invalid.",
                innerException: exception);
        }

        if (ContainsAny(
                message,
                "CK_ArticleReadModel_SourceVersion_NonNegative",
                "CK_ArticleReadModel_SourceVersion_Positive",
                "CK_ArticleReadModelTag_SourceVersion_NonNegative",
                "CK_ArticleReadModelMedia_SourceVersion_NonNegative",
                "CK_ArticleMediaProjectionState_SourceVersion_NonNegative"))
        {
            return new ReadingPersistenceException(
                code: "READING.INVALID_SOURCE_VERSION",
                message: "Source version must be greater than zero.",
                innerException: exception);
        }

        if (ContainsAny(
                message,
                "CK_ArticleReadModel_LastEventMessageId_Length",
                "CK_ArticleReadModel_LastEventMessageId_NotBlank"))
        {
            return new ReadingPersistenceException(
                code: "READING.INVALID_MESSAGE_ID",
                message: "Last event message id is invalid.",
                innerException: exception);
        }

        if (ContainsAny(
                message,
                "CK_ArticleReadModel_Counters_NonNegative",
                "CK_ArticleReadModel_ViewCount_NonNegative",
                "CK_ArticleReadModel_LikeCount_NonNegative",
                "CK_ArticleReadModel_CommentCount_NonNegative"))
        {
            return new ReadingPersistenceException(
                code: "READING.INVALID_COUNTER_VALUE",
                message: "Reading counters must not be negative.",
                innerException: exception);
        }

        if (message.Contains(
                "CK_ArticleReadModelTag_TagName_NotBlank",
                StringComparison.OrdinalIgnoreCase))
        {
            return new ReadingPersistenceException(
                code: "READING.INVALID_TAG_NAME",
                message: "Tag name is required.",
                innerException: exception);
        }

        if (message.Contains(
                "CK_ArticleReadModelMedia_Url_NotBlank",
                StringComparison.OrdinalIgnoreCase))
        {
            return new ReadingPersistenceException(
                code: "READING.INVALID_MEDIA_URL",
                message: "Media URL is required.",
                innerException: exception);
        }

        if (message.Contains(
                "CK_ArticleReadModelMedia_MediaPublicId_Length",
                StringComparison.OrdinalIgnoreCase))
        {
            return new ReadingPersistenceException(
                code: "READING.INVALID_MEDIA_PUBLIC_ID",
                message: "Media public id is invalid.",
                innerException: exception);
        }

        if (message.Contains(
                "CK_ArticleReadModelMedia_MediaType_NotBlank",
                StringComparison.OrdinalIgnoreCase))
        {
            return new ReadingPersistenceException(
                code: "READING.INVALID_MEDIA_TYPE",
                message: "Media type is required.",
                innerException: exception);
        }

        if (message.Contains(
                "CK_ArticleReadModelMedia_SortOrder_NonNegative",
                StringComparison.OrdinalIgnoreCase))
        {
            return new ReadingPersistenceException(
                code: "READING.INVALID_MEDIA_SORT_ORDER",
                message: "Media sort order must not be negative.",
                innerException: exception);
        }

        if (message.Contains(
                "CK_ArticleReadModel_PublicRequiresPublished",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.PUBLIC_REQUIRES_PUBLISHED",
                "Public Reading projection requires Published status and a publish timestamp.",
                exception);
        }

        if (ContainsAny(
                message,
                "CK_ArticleReadModel_PublishedAtUtc",
                "CK_ArticleReadModel_UpdatedAtUtc"))
        {
            return Reading(
                "READING.OBSOLETE_SOURCE_TIMESTAMP_CONSTRAINT",
                "Reading projection has an obsolete source timestamp constraint. Re-run reading table migration script to drop it.",
                exception);
        }

        if (message.Contains(
                "CK_ArticleReadModel_LastSyncedAtUtc",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_PROJECTION_SYNC_TIMESTAMP",
                "Reading projection sync timestamp is invalid.",
                exception);
        }

        if (message.Contains(
                "CK_ArticleReadModelTag_TagPublicId_Length",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_TAG_PUBLIC_ID",
                "Tag public id must be a valid 26-character public id.",
                exception);
        }

        return Reading(
            "READING.CONSTRAINT_VIOLATED",
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

    private static ReadingPersistenceException Reading(
        string code,
        string message,
        Exception exception)
    {
        return new ReadingPersistenceException(
            code: code,
            message: message,
            innerException: exception);
    }
}
