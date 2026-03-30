using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using Microsoft.Data.SqlClient;

namespace Content.Infrastructure.Persistence.Exceptions
{
    public sealed class ContentSqlExceptionTranslator : SqlExceptionTranslatorBase
    {
        public override Exception Translate(SqlException exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            return exception.Number switch
            {
                // =========================================================
                // CATEGORY
                // =========================================================
                54227 => new ContentPersistenceException(
                    code: "CONTENT.CONCURRENCY_CONFLICT",
                    message: "The content resource was modified by another operation.",
                    innerException: exception),

                54230 => new ContentPersistenceException(
                    code: "CONTENT.CATEGORY_DELETE_BLOCKED_BY_ARTICLES",
                    message: "Category cannot be deleted because active articles still reference it.",
                    innerException: exception),

                54231 => new ContentPersistenceException(
                    code: "CONTENT.CONCURRENCY_CONFLICT",
                    message: "The content resource was modified by another operation.",
                    innerException: exception),

                54232 => new ContentPersistenceException(
                    code: "CONTENT.CONCURRENCY_CONFLICT",
                    message: "The content resource was modified by another operation.",
                    innerException: exception),

                // =========================================================
                // TAG
                // =========================================================
                54240 => new ContentPersistenceException(
                    code: "CONTENT.TAG_PUBLIC_ID_REQUIRED",
                    message: "Tag public id is required.",
                    innerException: exception),

                54241 => new ContentPersistenceException(
                    code: "CONTENT.TAG_NAME_REQUIRED",
                    message: "Tag name is required.",
                    innerException: exception),

                54242 => new ContentPersistenceException(
                    code: "CONTENT.TAG_NAME_NORMALIZED_REQUIRED",
                    message: "Tag normalized name is required.",
                    innerException: exception),

                54243 => new ContentPersistenceException(
                    code: "CONTENT.TAG_NAME_NORMALIZED_ALREADY_EXISTS",
                    message: "Tag normalized name already exists.",
                    innerException: exception),

                54250 => new ContentPersistenceException(
                    code: "CONTENT.TAG_INVALID_TAG_ID",
                    message: "Tag id must be greater than zero.",
                    innerException: exception),

                54251 => new ContentPersistenceException(
                    code: "CONTENT.TAG_NAME_REQUIRED",
                    message: "Tag name is required.",
                    innerException: exception),

                54252 => new ContentPersistenceException(
                    code: "CONTENT.TAG_NAME_NORMALIZED_REQUIRED",
                    message: "Tag normalized name is required.",
                    innerException: exception),

                54253 => new ContentPersistenceException(
                    code: "CONTENT.TAG_NAME_NORMALIZED_ALREADY_EXISTS",
                    message: "Tag normalized name already exists.",
                    innerException: exception),

                54254 => new ContentPersistenceException(
                    code: "CONTENT.CONCURRENCY_CONFLICT",
                    message: "The content resource was modified by another operation.",
                    innerException: exception),

                54255 => new ContentPersistenceException(
                    code: "CONTENT.CONCURRENCY_CONFLICT",
                    message: "The content resource was modified by another operation.",
                    innerException: exception),

                54256 => new ContentPersistenceException(
                    code: "CONTENT.CONCURRENCY_CONFLICT",
                    message: "The content resource was modified by another operation.",
                    innerException: exception),

                // =========================================================
                // ARTICLE
                // =========================================================
                54260 => new ContentPersistenceException(
                    code: "CONTENT.ARTICLE_PUBLIC_ID_REQUIRED",
                    message: "Article public id is required.",
                    innerException: exception),

                54261 => new ContentPersistenceException(
                    code: "CONTENT.ARTICLE_AUTHOR_USER_ID_INVALID",
                    message: "Author user id must be greater than zero.",
                    innerException: exception),

                54262 => new ContentPersistenceException(
                    code: "CONTENT.ARTICLE_TITLE_REQUIRED",
                    message: "Article title is required.",
                    innerException: exception),

                54263 => new ContentPersistenceException(
                    code: "CONTENT.ARTICLE_BODY_REQUIRED",
                    message: "Article body is required.",
                    innerException: exception),

                54264 => new ContentPersistenceException(
                    code: "CONTENT.INVALID_STATE_TRANSITION",
                    message: "The requested article state transition is not allowed.",
                    innerException: exception),

                54265 => new ContentPersistenceException(
                    code: "CONTENT.CATEGORY_NOT_FOUND",
                    message: "Category does not exist, is deleted, or inactive.",
                    innerException: exception),

                54266 => new ContentPersistenceException(
                    code: "CONTENT.ARTICLE_PUBLIC_ID_ALREADY_EXISTS",
                    message: "Article public id already exists.",
                    innerException: exception),

                54270 => new ContentPersistenceException(
                    code: "CONTENT.ARTICLE_INVALID_ARTICLE_ID",
                    message: "Article id must be greater than zero.",
                    innerException: exception),

                54271 => new ContentPersistenceException(
                    code: "CONTENT.ARTICLE_TITLE_REQUIRED",
                    message: "Article title is required.",
                    innerException: exception),

                54272 => new ContentPersistenceException(
                    code: "CONTENT.ARTICLE_BODY_REQUIRED",
                    message: "Article body is required.",
                    innerException: exception),

                54273 => new ContentPersistenceException(
                    code: "CONTENT.CATEGORY_NOT_FOUND",
                    message: "Category does not exist, is deleted, or inactive.",
                    innerException: exception),

                54274 => new ContentPersistenceException(
                    code: "CONTENT.ARTICLE_ALREADY_ARCHIVED",
                    message: "Cannot update archived article. Restore it first.",
                    innerException: exception),

                54275 => new ContentPersistenceException(
                    code: "CONTENT.CONCURRENCY_CONFLICT",
                    message: "The content resource was modified by another operation.",
                    innerException: exception),

                54280 => new ContentPersistenceException(
                    code: "CONTENT.ARTICLE_ALREADY_DELETED",
                    message: "Cannot publish deleted article.",
                    innerException: exception),

                54281 => new ContentPersistenceException(
                    code: "CONTENT.ARTICLE_ALREADY_ARCHIVED",
                    message: "Cannot publish archived article.",
                    innerException: exception),

                54282 => new ContentPersistenceException(
                    code: "CONTENT.VALIDATION_FAILED",
                    message: "Cannot publish article without title and content.",
                    innerException: exception),

                54283 => new ContentPersistenceException(
                    code: "CONTENT.CONCURRENCY_CONFLICT",
                    message: "The content resource was modified by another operation.",
                    innerException: exception),

                54284 => new ContentPersistenceException(
                    code: "CONTENT.CONCURRENCY_CONFLICT",
                    message: "The content resource was modified by another operation.",
                    innerException: exception),

                54285 => new ContentPersistenceException(
                    code: "CONTENT.CONCURRENCY_CONFLICT",
                    message: "The content resource was modified by another operation.",
                    innerException: exception),

                54286 => new ContentPersistenceException(
                    code: "CONTENT.CONCURRENCY_CONFLICT",
                    message: "The content resource was modified by another operation.",
                    innerException: exception),

                54287 => new ContentPersistenceException(
                    code: "CONTENT.CONCURRENCY_CONFLICT",
                    message: "The content resource was modified by another operation.",
                    innerException: exception),

                _ => exception
            };
        }
    }
}