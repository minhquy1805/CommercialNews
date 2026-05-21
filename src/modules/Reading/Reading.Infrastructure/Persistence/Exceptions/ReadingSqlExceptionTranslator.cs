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
            2601 or 2627 => MapUniqueConstraint(exception),
            547 => MapForeignKeyOrCheckConstraint(exception),

            _ => new ReadingPersistenceException(
                code: "READING.PERSISTENCE_ERROR",
                message: "An unexpected SQL persistence error occurred.",
                innerException: exception)
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

        if (message.Contains(
                "UX_ArticleReadModel_Slug_Public",
                StringComparison.OrdinalIgnoreCase))
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

        return new ReadingPersistenceException(
            code: "READING.UNIQUE_CONSTRAINT_VIOLATED",
            message: "A persistence uniqueness constraint was violated.",
            innerException: exception);
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

        if (message.Contains(
                "CK_ArticleReadModel_ArticlePublicId_NotBlank",
                StringComparison.OrdinalIgnoreCase))
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

        if (message.Contains(
                "CK_ArticleReadModel_SourceVersion_Positive",
                StringComparison.OrdinalIgnoreCase))
        {
            return new ReadingPersistenceException(
                code: "READING.INVALID_SOURCE_VERSION",
                message: "Source version must be greater than zero.",
                innerException: exception);
        }

        if (message.Contains(
                "CK_ArticleReadModel_LastEventMessageId_NotBlank",
                StringComparison.OrdinalIgnoreCase))
        {
            return new ReadingPersistenceException(
                code: "READING.INVALID_MESSAGE_ID",
                message: "Last event message id is invalid.",
                innerException: exception);
        }

        if (message.Contains(
                "CK_ArticleReadModel_ViewCount_NonNegative",
                StringComparison.OrdinalIgnoreCase) ||
            message.Contains(
                "CK_ArticleReadModel_LikeCount_NonNegative",
                StringComparison.OrdinalIgnoreCase) ||
            message.Contains(
                "CK_ArticleReadModel_CommentCount_NonNegative",
                StringComparison.OrdinalIgnoreCase))
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

        return new ReadingPersistenceException(
            code: "READING.CONSTRAINT_VIOLATED",
            message: "A foreign key or check constraint was violated.",
            innerException: exception);
    }
}