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
            /*
              =========================================================
              Bootstrap / required persistence objects
              =========================================================
            */

            56001 or 56101 or 58201 => Reading(
                "READING.DATABASE_NOT_FOUND",
                "Database CommercialNews does not exist.",
                exception),

            56002 or 56102 or 58202 => Reading(
                "READING.SCHEMA_NOT_FOUND",
                "Schema reading does not exist.",
                exception),

            56103 or 56104 or 56105 or 56106 or 56107 or 56108 or 56109
                or 58203 or 58204 or 58205 or 58206 or 58207 or 58208 or 58209 => Reading(
                "READING.PROJECTION_STORE_NOT_FOUND",
                "A required Reading projection table does not exist.",
                exception),

            /*
              =========================================================
              Public query validation
              =========================================================
            */

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

            /*
              =========================================================
              Content projection validation
              =========================================================
            */

            58250 or 58260 or 58280 or 58300 or 58320 or 58340 or 58360 => Reading(
                "READING.INVALID_ARTICLE_ID",
                "Article id must be greater than zero.",
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

            58256 or 58262 => Reading(
                "READING.INVALID_CONTENT_SOURCE_VERSION",
                "Content source version must be greater than zero.",
                exception),

            58257 => Reading(
                "READING.INVALID_COVER_MEDIA_ID",
                "Cover media id must be greater than zero when provided.",
                exception),

            58258 or 58380 => Reading(
                "READING.INVALID_AUTHOR_USER_ID",
                "Author user id must be greater than zero.",
                exception),

            /*
              =========================================================
              SEO route projection validation
              =========================================================
            */

            58270 => Reading(
                "READING.INVALID_SEO_ROUTE_SCOPE",
                "SEO route scope is required.",
                exception),

            58271 => Reading(
                "READING.INVALID_SEO_ROUTE_RESOURCE_TYPE",
                "SEO route resource type is required.",
                exception),

            58272 => Reading(
                "READING.INVALID_SEO_ROUTE_RESOURCE_PUBLIC_ID",
                "SEO route resource public id must be a valid 26-character public id.",
                exception),

            58273 => Reading(
                "READING.INVALID_SEO_ROUTE_SLUG",
                "SEO route slug is required.",
                exception),

            58274 => Reading(
                "READING.INVALID_SEO_ROUTE_SOURCE_VERSION",
                "SEO route source version must be greater than zero.",
                exception),

            58275 => Reading(
                "READING.UNSUPPORTED_SEO_ROUTE_SCOPE",
                "Only public SEO scope is supported by the Reading article route projection.",
                exception),

            58276 => Reading(
                "READING.UNSUPPORTED_SEO_ROUTE_RESOURCE_TYPE",
                "Only Article SEO resource type is supported by the Reading article route projection.",
                exception),

            /*
              =========================================================
              Counter refresh validation
              =========================================================
            */

            58281 => Reading(
                "READING.INVALID_COUNTER_VALUE",
                "Reading counters must not be negative.",
                exception),

            /*
              =========================================================
              SEO metadata projection validation
              =========================================================
            */

            58290 => Reading(
                "READING.INVALID_SEO_METADATA_SCOPE",
                "SEO metadata scope is required.",
                exception),

            58291 => Reading(
                "READING.INVALID_SEO_METADATA_RESOURCE_TYPE",
                "SEO metadata resource type is required.",
                exception),

            58292 => Reading(
                "READING.UNSUPPORTED_SEO_METADATA_SCOPE",
                "Only public SEO scope is supported by the Reading article metadata projection.",
                exception),

            58293 => Reading(
                "READING.UNSUPPORTED_SEO_METADATA_RESOURCE_TYPE",
                "Only Article SEO resource type is supported by the Reading article metadata projection.",
                exception),

            58294 => Reading(
                "READING.INVALID_SEO_METADATA_RESOURCE_PUBLIC_ID",
                "SEO metadata resource public id must be a valid 26-character public id.",
                exception),

            58295 => Reading(
                "READING.INVALID_SEO_METADATA_SOURCE_VERSION",
                "SEO metadata source version must be greater than zero.",
                exception),

            /*
              =========================================================
              Media projection validation
              =========================================================
            */

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

            58305 or 58325 or 58343 => Reading(
                "READING.INVALID_MEDIA_SORT_ORDER",
                "Media sort order is invalid.",
                exception),

            58306 or 58326 or 58341 or 58362 => Reading(
                "READING.INVALID_MEDIA_SOURCE_VERSION",
                "Media source version must be greater than zero.",
                exception),

            58307 => Reading(
                "READING.INVALID_MEDIA_PRIMARY_FLAG",
                "Media primary flag is required.",
                exception),

            58342 => Reading(
                "READING.MEDIA_REORDER_ITEMS_REQUIRED",
                "Media reorder items are required.",
                exception),

            58344 => Reading(
                "READING.DUPLICATE_MEDIA_REORDER_ITEM",
                "Media reorder items must not contain duplicate media ids.",
                exception),

            /*
              =========================================================
              Identity author profile projection validation
              =========================================================
            */

            58381 => Reading(
                "READING.INVALID_AUTHOR_USER_PUBLIC_ID",
                "Author user public id must be a valid 26-character public id.",
                exception),

            58382 => Reading(
                "READING.INVALID_AUTHOR_PROFILE_SOURCE_VERSION",
                "Author profile source version must be greater than zero.",
                exception),

            58383 => Reading(
                "READING.INVALID_AUTHOR_PROFILE_MESSAGE_ID",
                "Author profile message id must be a valid 26-character value.",
                exception),

            58384 => Reading(
                "READING.INVALID_AUTHOR_PROFILE_SOURCE_OCCURRED_AT_UTC",
                "Author profile source occurred timestamp is required.",
                exception),

            /*
              =========================================================
              SQL Server constraints
              =========================================================
            */

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
            return Reading(
                "READING.ARTICLE_PROJECTION_ALREADY_EXISTS",
                "Article read model already exists.",
                exception);
        }

        if (message.Contains(
                "UQ_ArticleReadModel_ArticlePublicId",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.ARTICLE_PUBLIC_ID_ALREADY_EXISTS",
                "Article public id already exists in the Reading projection.",
                exception);
        }

        if (message.Contains(
                "PK_ArticleReadModelTag",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.ARTICLE_TAG_PROJECTION_ALREADY_EXISTS",
                "Article tag projection already exists.",
                exception);
        }

        if (message.Contains(
                "PK_ArticleReadModelMedia",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.ARTICLE_MEDIA_PROJECTION_ALREADY_EXISTS",
                "Article media projection already exists.",
                exception);
        }

        if (message.Contains(
                "UX_ArticleReadModelMedia_Article_Primary",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.ARTICLE_MEDIA_PRIMARY_ALREADY_EXISTS",
                "An article can have at most one primary projected media item.",
                exception);
        }

        if (message.Contains(
                "PK_ArticleMediaProjectionState",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.MEDIA_PROJECTION_STATE_ALREADY_EXISTS",
                "Article media projection state already exists.",
                exception);
        }

        if (message.Contains(
                "PK_ArticleSeoRouteProjection",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.SEO_ROUTE_PROJECTION_ALREADY_EXISTS",
                "Article SEO route projection already exists.",
                exception);
        }

        if (message.Contains(
                "PK_ArticleSeoMetadataProjection",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.SEO_METADATA_PROJECTION_ALREADY_EXISTS",
                "Article SEO metadata projection already exists.",
                exception);
        }

        if (message.Contains(
                "PK_AuthorProfileProjection",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.AUTHOR_PROFILE_PROJECTION_ALREADY_EXISTS",
                "Author profile projection already exists.",
                exception);
        }

        if (message.Contains(
                "UQ_AuthorProfileProjection_AuthorUserPublicId",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.AUTHOR_PROFILE_PUBLIC_ID_ALREADY_EXISTS",
                "Author user public id already exists in the Reading projection.",
                exception);
        }

        return Reading(
            "READING.UNIQUE_CONSTRAINT_VIOLATED",
            "A persistence uniqueness constraint was violated.",
            exception);
    }

    private static Exception MapForeignKeyOrCheckConstraint(SqlException exception)
    {
        string message = exception.Message;

        /*
          =========================================================
          Foreign keys
          =========================================================
        */

        if (message.Contains(
                "FK_ArticleReadModelTag_ArticleReadModel",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.ARTICLE_PROJECTION_NOT_FOUND",
                "The referenced article read model does not exist.",
                exception);
        }

        /*
          Retained only for older development databases.
          Current Media projection schema intentionally has no FK to
          ArticleReadModel because Media events may arrive first.
        */
        if (message.Contains(
                "FK_ArticleReadModelMedia_ArticleReadModel",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.OBSOLETE_MEDIA_ARTICLE_FOREIGN_KEY",
                "Reading media projection still contains an obsolete article foreign key. Re-run the Reading table migration script.",
                exception);
        }

        /*
          =========================================================
          ArticleReadModel constraints
          =========================================================
        */

        if (message.Contains(
                "CK_ArticleReadModel_ArticlePublicId_Length",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_ARTICLE_PUBLIC_ID",
                "Article public id must be a valid 26-character public id.",
                exception);
        }

        if (message.Contains(
                "CK_ArticleReadModel_LastEventMessageId_Length",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_MESSAGE_ID",
                "Content projection message id must be a valid 26-character value.",
                exception);
        }

        if (message.Contains(
                "CK_ArticleReadModel_Title_NotBlank",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_TITLE",
                "Article title is required.",
                exception);
        }

        if (message.Contains(
                "CK_ArticleReadModel_Summary_NotBlank",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_SUMMARY",
                "Article summary is required.",
                exception);
        }

        if (message.Contains(
                "CK_ArticleReadModel_Body_NotBlank",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_BODY",
                "Article body is required.",
                exception);
        }

        if (message.Contains(
                "CK_ArticleReadModel_Status",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_SOURCE_STATUS",
                "Source article status is invalid.",
                exception);
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

        if (message.Contains(
                "CK_ArticleReadModel_LastSyncedAtUtc",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_PROJECTION_SYNC_TIMESTAMP",
                "Reading Content projection synchronization timestamp is invalid.",
                exception);
        }

        if (message.Contains(
                "CK_ArticleReadModel_SourceVersion_NonNegative",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_CONTENT_SOURCE_VERSION",
                "Content source version must be non-negative.",
                exception);
        }

        if (message.Contains(
                "CK_ArticleReadModel_Counters_NonNegative",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_COUNTER_VALUE",
                "Reading counters must not be negative.",
                exception);
        }

        if (message.Contains(
                "CK_ArticleReadModel_SeoIndexableRequiresActive",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_SEO_ROUTE_INDEXABLE_STATE",
                "SEO indexed state requires an active projected route.",
                exception);
        }

        /*
          Older development databases may still contain these obsolete
          source/projection timestamp comparison constraints.
        */
        if (ContainsAny(
                message,
                "CK_ArticleReadModel_PublishedAtUtc",
                "CK_ArticleReadModel_UpdatedAtUtc"))
        {
            return Reading(
                "READING.OBSOLETE_SOURCE_TIMESTAMP_CONSTRAINT",
                "Reading projection has an obsolete source timestamp constraint. Re-run the Reading table migration script.",
                exception);
        }

        /*
          =========================================================
          ArticleReadModelTag constraints
          =========================================================
        */

        if (message.Contains(
                "CK_ArticleReadModelTag_TagPublicId_Length",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_TAG_PUBLIC_ID",
                "Tag public id must be a valid 26-character public id.",
                exception);
        }

        if (message.Contains(
                "CK_ArticleReadModelTag_Name_NotBlank",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_TAG_NAME",
                "Tag name is required.",
                exception);
        }

        if (message.Contains(
                "CK_ArticleReadModelTag_SourceVersion_NonNegative",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_TAG_SOURCE_VERSION",
                "Tag projection source version must be non-negative.",
                exception);
        }

        /*
          =========================================================
          ArticleReadModelMedia constraints
          =========================================================
        */

        if (message.Contains(
                "CK_ArticleReadModelMedia_ArticleId_Positive",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_ARTICLE_ID",
                "Article id must be greater than zero.",
                exception);
        }

        if (message.Contains(
                "CK_ArticleReadModelMedia_MediaId_Positive",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_MEDIA_ID",
                "Media id must be greater than zero.",
                exception);
        }

        if (message.Contains(
                "CK_ArticleReadModelMedia_MediaPublicId_Length",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_MEDIA_PUBLIC_ID",
                "Media public id must be a valid 26-character public id.",
                exception);
        }

        if (message.Contains(
                "CK_ArticleReadModelMedia_Url_NotBlank",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_MEDIA_URL",
                "Media URL is required.",
                exception);
        }

        if (message.Contains(
                "CK_ArticleReadModelMedia_MediaType_NotBlank",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_MEDIA_TYPE",
                "Media type is required.",
                exception);
        }

        if (message.Contains(
                "CK_ArticleReadModelMedia_SortOrder_NonNegative",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_MEDIA_SORT_ORDER",
                "Media sort order must not be negative.",
                exception);
        }

        if (message.Contains(
                "CK_ArticleReadModelMedia_SourceVersion_NonNegative",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_MEDIA_SOURCE_VERSION",
                "Media source version must be non-negative.",
                exception);
        }

        /*
          =========================================================
          ArticleMediaProjectionState constraints
          =========================================================
        */

        if (message.Contains(
                "CK_ArticleMediaProjectionState_ArticleId_Positive",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_ARTICLE_ID",
                "Article id must be greater than zero.",
                exception);
        }

        if (message.Contains(
                "CK_ArticleMediaProjectionState_SourceVersion_NonNegative",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_MEDIA_SOURCE_VERSION",
                "Media source version must be non-negative.",
                exception);
        }

        if (message.Contains(
                "CK_ArticleMediaProjectionState_LastEventMessageId_Length",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_MEDIA_MESSAGE_ID",
                "Media projection message id must be a valid 26-character value.",
                exception);
        }

        /*
          =========================================================
          ArticleSeoRouteProjection constraints
          =========================================================
        */

        if (message.Contains(
                "CK_ArticleSeoRouteProjection_Scope_NotBlank",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_SEO_ROUTE_SCOPE",
                "SEO route scope is required.",
                exception);
        }

        if (message.Contains(
                "CK_ArticleSeoRouteProjection_ResourceType_NotBlank",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_SEO_ROUTE_RESOURCE_TYPE",
                "SEO route resource type is required.",
                exception);
        }

        if (message.Contains(
                "CK_ArticleSeoRouteProjection_ResourcePublicId_Length",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_SEO_ROUTE_RESOURCE_PUBLIC_ID",
                "SEO route resource public id must be a valid 26-character public id.",
                exception);
        }

        if (message.Contains(
                "CK_ArticleSeoRouteProjection_Slug_NotBlank",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_SEO_ROUTE_SLUG",
                "SEO route slug is required.",
                exception);
        }

        if (message.Contains(
                "CK_ArticleSeoRouteProjection_SourceVersion_NonNegative",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_SEO_ROUTE_SOURCE_VERSION",
                "SEO route source version must be non-negative.",
                exception);
        }

        if (message.Contains(
                "CK_ArticleSeoRouteProjection_LastEventMessageId_Length",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_SEO_ROUTE_MESSAGE_ID",
                "SEO route message id must be a valid 26-character value.",
                exception);
        }

        if (message.Contains(
                "CK_ArticleSeoRouteProjection_IndexableRequiresActive",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_SEO_ROUTE_INDEXABLE_STATE",
                "SEO indexed state requires an active projected route.",
                exception);
        }

        /*
          =========================================================
          ArticleSeoMetadataProjection constraints
          =========================================================
        */

        if (message.Contains(
                "CK_ArticleSeoMetadataProjection_Scope_NotBlank",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_SEO_METADATA_SCOPE",
                "SEO metadata scope is required.",
                exception);
        }

        if (message.Contains(
                "CK_ArticleSeoMetadataProjection_ResourceType_NotBlank",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_SEO_METADATA_RESOURCE_TYPE",
                "SEO metadata resource type is required.",
                exception);
        }

        if (message.Contains(
                "CK_ArticleSeoMetadataProjection_ResourcePublicId_Length",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_SEO_METADATA_RESOURCE_PUBLIC_ID",
                "SEO metadata resource public id must be a valid 26-character public id.",
                exception);
        }

        if (message.Contains(
                "CK_ArticleSeoMetadataProjection_SourceVersion_NonNegative",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_SEO_METADATA_SOURCE_VERSION",
                "SEO metadata source version must be non-negative.",
                exception);
        }

        if (message.Contains(
                "CK_ArticleSeoMetadataProjection_LastEventMessageId_Length",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_SEO_METADATA_MESSAGE_ID",
                "SEO metadata message id must be a valid 26-character value.",
                exception);
        }

        /*
          =========================================================
          AuthorProfileProjection constraints
          =========================================================
        */

        if (message.Contains(
                "CK_AuthorProfileProjection_AuthorUserId_Positive",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_AUTHOR_USER_ID",
                "Author user id must be greater than zero.",
                exception);
        }

        if (message.Contains(
                "CK_AuthorProfileProjection_AuthorUserPublicId_Length",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_AUTHOR_USER_PUBLIC_ID",
                "Author user public id must be a valid 26-character public id.",
                exception);
        }

        if (message.Contains(
                "CK_AuthorProfileProjection_SourceVersion_Positive",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_AUTHOR_PROFILE_SOURCE_VERSION",
                "Author profile source version must be greater than zero.",
                exception);
        }

        if (message.Contains(
                "CK_AuthorProfileProjection_LastEventMessageId_Length",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.INVALID_AUTHOR_PROFILE_MESSAGE_ID",
                "Author profile message id must be a valid 26-character value.",
                exception);
        }

        if (message.Contains(
                "CK_AuthorProfileProjection_LastAppliedMessageId_Length",
                StringComparison.OrdinalIgnoreCase))
        {
            return Reading(
                "READING.OBSOLETE_AUTHOR_PROFILE_MESSAGE_ID_CONSTRAINT",
                "Reading author profile projection still contains an obsolete LastAppliedMessageId constraint. Re-run the Reading table migration script.",
                exception);
        }

        if (ContainsAny(
                message,
                "CK_AuthorProfileProjection_LastSyncedAtUtc",
                "CK_AuthorProfileProjection_UpdatedAtUtc"))
        {
            return Reading(
                "READING.OBSOLETE_AUTHOR_PROFILE_TIMESTAMP_CONSTRAINT",
                "Reading author profile projection has obsolete timestamp constraints. Re-run the Reading table migration script.",
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
