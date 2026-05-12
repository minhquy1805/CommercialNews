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
                // BOOTSTRAP / SCHEMA
                // =========================================================
                54201 => Content(
                    "CONTENT.DATABASE_NOT_FOUND",
                    "Database CommercialNews does not exist.",
                    exception),

                54202 => Content(
                    "CONTENT.SCHEMA_NOT_FOUND",
                    "Schema content does not exist.",
                    exception),

                // =========================================================
                // CATEGORY
                // =========================================================
                54210 => Content(
                    "CONTENT.CATEGORY_PUBLIC_ID_REQUIRED",
                    "Category public id is required.",
                    exception),

                54211 => Content(
                    "CONTENT.CATEGORY_NAME_REQUIRED",
                    "Category name is required.",
                    exception),

                54212 => Content(
                    "CONTENT.CATEGORY_NAME_NORMALIZED_REQUIRED",
                    "Category normalized name is required.",
                    exception),

                54213 => Content(
                    "CONTENT.CATEGORY_DISPLAY_ORDER_INVALID",
                    "Category display order must be greater than or equal to zero.",
                    exception),

                54214 => Content(
                    "CONTENT.CATEGORY_PARENT_NOT_FOUND",
                    "Parent category does not exist or was deleted.",
                    exception),

                54215 => Content(
                    "CONTENT.CATEGORY_NAME_NORMALIZED_ALREADY_EXISTS",
                    "Category normalized name already exists.",
                    exception),

                54220 => Content(
                    "CONTENT.CATEGORY_INVALID_CATEGORY_ID",
                    "Category id must be greater than zero.",
                    exception),

                54221 => Content(
                    "CONTENT.CATEGORY_NAME_REQUIRED",
                    "Category name is required.",
                    exception),

                54222 => Content(
                    "CONTENT.CATEGORY_NAME_NORMALIZED_REQUIRED",
                    "Category normalized name is required.",
                    exception),

                54223 => Content(
                    "CONTENT.CATEGORY_DISPLAY_ORDER_INVALID",
                    "Category display order must be greater than or equal to zero.",
                    exception),

                54224 => Content(
                    "CONTENT.CATEGORY_PARENT_SELF_REFERENCE",
                    "Category cannot be its own parent.",
                    exception),

                54225 => Content(
                    "CONTENT.CATEGORY_PARENT_NOT_FOUND",
                    "Parent category does not exist or was deleted.",
                    exception),

                54226 => Content(
                    "CONTENT.CATEGORY_NAME_NORMALIZED_ALREADY_EXISTS",
                    "Category normalized name already exists.",
                    exception),

                54227 => Content(
                    "CONTENT.CONCURRENCY_CONFLICT",
                    "Category update failed. Record was not found, deleted, or version mismatched.",
                    exception),

                54230 => Content(
                    "CONTENT.CATEGORY_DELETE_BLOCKED_BY_ARTICLES",
                    "Category cannot be deleted because active articles still reference it.",
                    exception),

                54231 => Content(
                    "CONTENT.CONCURRENCY_CONFLICT",
                    "Category delete failed. Record was not found, already deleted, or version mismatched.",
                    exception),

                54232 => Content(
                    "CONTENT.CONCURRENCY_CONFLICT",
                    "Category restore failed. Record was not found, not deleted, or version mismatched.",
                    exception),

                // =========================================================
                // TAG
                // =========================================================
                54240 => Content(
                    "CONTENT.TAG_PUBLIC_ID_REQUIRED",
                    "Tag public id is required.",
                    exception),

                54241 => Content(
                    "CONTENT.TAG_NAME_REQUIRED",
                    "Tag name is required.",
                    exception),

                54242 => Content(
                    "CONTENT.TAG_NAME_NORMALIZED_REQUIRED",
                    "Tag normalized name is required.",
                    exception),

                54243 => Content(
                    "CONTENT.TAG_NAME_NORMALIZED_ALREADY_EXISTS",
                    "Tag normalized name already exists.",
                    exception),

                54250 => Content(
                    "CONTENT.TAG_INVALID_TAG_ID",
                    "Tag id must be greater than zero.",
                    exception),

                54251 => Content(
                    "CONTENT.TAG_NAME_REQUIRED",
                    "Tag name is required.",
                    exception),

                54252 => Content(
                    "CONTENT.TAG_NAME_NORMALIZED_REQUIRED",
                    "Tag normalized name is required.",
                    exception),

                54253 => Content(
                    "CONTENT.TAG_NAME_NORMALIZED_ALREADY_EXISTS",
                    "Tag normalized name already exists.",
                    exception),

                54254 => Content(
                    "CONTENT.CONCURRENCY_CONFLICT",
                    "Tag update failed. Record was not found, deleted, or version mismatched.",
                    exception),

                54255 => Content(
                    "CONTENT.CONCURRENCY_CONFLICT",
                    "Tag delete failed. Record was not found, already deleted, or version mismatched.",
                    exception),

                54256 => Content(
                    "CONTENT.CONCURRENCY_CONFLICT",
                    "Tag restore failed. Record was not found, not deleted, or version mismatched.",
                    exception),

                // =========================================================
                // ARTICLE
                // =========================================================
                54260 => Content(
                    "CONTENT.ARTICLE_PUBLIC_ID_INVALID",
                    "Article public id must be a 26-character ULID.",
                    exception),

                54261 => Content(
                    "CONTENT.ARTICLE_AUTHOR_USER_ID_INVALID",
                    "Author user id must be greater than zero.",
                    exception),

                54262 => Content(
                    "CONTENT.ARTICLE_TITLE_REQUIRED",
                    "Article title is required.",
                    exception),

                54263 => Content(
                    "CONTENT.ARTICLE_SUMMARY_REQUIRED",
                    "Article summary is required.",
                    exception),

                54264 => Content(
                    "CONTENT.ARTICLE_BODY_REQUIRED",
                    "Article body is required.",
                    exception),

                54265 => Content(
                    "CONTENT.ARTICLE_CATEGORY_ID_INVALID",
                    "Category id must be greater than zero.",
                    exception),

                54266 => Content(
                    "CONTENT.CATEGORY_INACTIVE_OR_DELETED",
                    "Category does not exist, is deleted, or inactive.",
                    exception),

                54267 => Content(
                    "CONTENT.ARTICLE_PUBLIC_ID_ALREADY_EXISTS",
                    "Article public id already exists.",
                    exception),

                54268 => Content(
                    "CONTENT.ARTICLE_CREATED_BY_USER_ID_INVALID",
                    "Created by user id must be greater than zero.",
                    exception),

                54269 => Content(
                    "CONTENT.ARTICLE_CATEGORY_INACTIVE_OR_DELETED",
                    "Cannot publish article because category is deleted or inactive.",
                    exception),

                54270 => Content(
                    "CONTENT.ARTICLE_INVALID_ARTICLE_ID",
                    "Article id must be greater than zero.",
                    exception),

                54271 => Content(
                    "CONTENT.ARTICLE_TITLE_REQUIRED",
                    "Article title is required.",
                    exception),

                54272 => Content(
                    "CONTENT.ARTICLE_SUMMARY_REQUIRED",
                    "Article summary is required.",
                    exception),

                54273 => Content(
                    "CONTENT.ARTICLE_BODY_REQUIRED",
                    "Article body is required.",
                    exception),

                54274 => Content(
                    "CONTENT.ARTICLE_CATEGORY_ID_INVALID",
                    "Category id must be greater than zero.",
                    exception),

                54275 => Content(
                    "CONTENT.CATEGORY_INACTIVE_OR_DELETED",
                    "Category does not exist, is deleted, or inactive.",
                    exception),

                54276 => Content(
                    "CONTENT.CONCURRENCY_CONFLICT",
                    "Article update failed. Record was not found, deleted, not draft, or version mismatched.",
                    exception),

                54280 => Content(
                    "CONTENT.ARTICLE_ALREADY_SOFT_DELETED",
                    "Cannot publish deleted article.",
                    exception),

                54281 => Content(
                    "CONTENT.ARTICLE_ALREADY_ARCHIVED",
                    "Cannot publish archived article.",
                    exception),

                54282 => Content(
                    "CONTENT.ARTICLE_NOT_PUBLISHABLE",
                    "Cannot publish article without title, summary, and body.",
                    exception),

                54283 => Content(
                    "CONTENT.CONCURRENCY_CONFLICT",
                    "Article publish failed. Record was not found, not draft, deleted, or version mismatched.",
                    exception),

                54284 => Content(
                    "CONTENT.ARTICLE_INVALID_ARTICLE_ID",
                    "Article id must be greater than zero.",
                    exception),

                54285 => Content(
                    "CONTENT.ARTICLE_INVALID_VERSION",
                    "Expected version must be greater than zero.",
                    exception),

                54286 => Content(
                    "CONTENT.UNPUBLISH_REASON_REQUIRED",
                    "Reason is required for unpublish.",
                    exception),

                54287 => Content(
                    "CONTENT.CONCURRENCY_CONFLICT",
                    "Article unpublish failed. Record was not found, not published, deleted, or version mismatched.",
                    exception),

                54288 => Content(
                    "CONTENT.CONCURRENCY_CONFLICT",
                    "Article archive failed. Record was not found, not draft/published, deleted, or version mismatched.",
                    exception),

                54289 => Content(
                    "CONTENT.CONCURRENCY_CONFLICT",
                    "Article soft-delete failed. Record was not found, already soft-deleted, or version mismatched.",
                    exception),

                // =========================================================
                // ARTICLE TAG
                // =========================================================
                54290 => Content(
                    "CONTENT.ARTICLE_TAG_INVALID_ARTICLE_ID",
                    "Article id must be greater than zero.",
                    exception),

                54291 => Content(
                    "CONTENT.ARTICLE_TAG_INVALID_TAG_ID",
                    "Tag id must be greater than zero.",
                    exception),

                54292 => Content(
                    "CONTENT.ARTICLE_TAG_ARTICLE_NOT_DRAFT",
                    "Article does not exist, was deleted, or is not draft.",
                    exception),

                54293 => Content(
                    "CONTENT.ARTICLE_TAG_TAG_NOT_ATTACHABLE",
                    "Tag does not exist, is deleted, or inactive.",
                    exception),

                54294 => Content(
                    "CONTENT.ARTICLE_TAG_ALREADY_EXISTS",
                    "Article tag attachment already exists.",
                    exception),

                54295 => Content(
                    "CONTENT.ARTICLE_TAG_INVALID_ARTICLE_ID",
                    "Article id must be greater than zero.",
                    exception),

                54296 => Content(
                    "CONTENT.ARTICLE_TAG_INVALID_TAG_ID",
                    "Tag id must be greater than zero.",
                    exception),

                54297 => Content(
                    "CONTENT.ARTICLE_TAG_ARTICLE_NOT_DRAFT",
                    "Article does not exist, was deleted, or is not draft.",
                    exception),

                54298 => Content(
                    "CONTENT.ARTICLE_TAG_INVALID_ARTICLE_ID",
                    "Article id must be greater than zero.",
                    exception),

                54299 => Content(
                    "CONTENT.ARTICLE_TAG_ARTICLE_NOT_DRAFT",
                    "Article does not exist, was deleted, or is not draft.",
                    exception),

                // =========================================================
                // ARTICLE REVISION
                // =========================================================
                54300 => Content(
                    "CONTENT.ARTICLE_REVISION_INVALID_ARTICLE_ID",
                    "Article does not exist for revision.",
                    exception),

                54301 => Content(
                    "CONTENT.ARTICLE_REVISION_INVALID_EDITOR_USER_ID",
                    "Edited by user id must be greater than zero.",
                    exception),

                54302 => Content(
                    "CONTENT.ARTICLE_REVISION_INVALID_ARTICLE_VERSION",
                    "Article version must be greater than zero when provided.",
                    exception),

                54303 => Content(
                    "CONTENT.ARTICLE_REVISION_PREVIOUS_SNAPSHOT_REQUIRED",
                    "Article revision requires at least one previous value.",
                    exception),

                // =========================================================
                // ARTICLE LIFECYCLE EVENT
                // =========================================================
                54310 => Content(
                    "CONTENT.ARTICLE_LIFECYCLE_EVENT_INVALID_ARTICLE_ID",
                    "Article does not exist for lifecycle event.",
                    exception),

                54311 => Content(
                    "CONTENT.ARTICLE_LIFECYCLE_EVENT_ACTION_TYPE_INVALID",
                    "Lifecycle action type is invalid.",
                    exception),

                54312 => Content(
                    "CONTENT.ARTICLE_LIFECYCLE_EVENT_STATUS_INVALID",
                    "Lifecycle from status is invalid.",
                    exception),

                54313 => Content(
                    "CONTENT.ARTICLE_LIFECYCLE_EVENT_STATUS_INVALID",
                    "Lifecycle to status is invalid.",
                    exception),

                54314 => Content(
                    "CONTENT.ARTICLE_LIFECYCLE_EVENT_UNPUBLISH_REASON_REQUIRED",
                    "Reason is required for unpublish lifecycle event.",
                    exception),

                54315 => Content(
                    "CONTENT.ARTICLE_LIFECYCLE_EVENT_INVALID_ARTICLE_VERSION",
                    "Article version must be greater than zero.",
                    exception),

                54316 => Content(
                    "CONTENT.ARTICLE_LIFECYCLE_EVENT_INVALID_ACTOR_USER_ID",
                    "Actor user id must be greater than zero.",
                    exception),

                _ => exception
            };
        }

        private static ContentPersistenceException Content(
            string code,
            string message,
            Exception innerException)
        {
            return new ContentPersistenceException(
                code: code,
                message: message,
                innerException: innerException);
        }
    }
}