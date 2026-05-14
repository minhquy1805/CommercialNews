using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Content.Application.Errors
{
    public static class ContentErrors
    {
        public static readonly Error ValidationFailed =
            Error.Validation(
                code: "CONTENT.VALIDATION_FAILED",
                message: "One or more content validations failed.");

        public static readonly Error InvalidRequest =
            Error.Validation(
                code: "CONTENT.INVALID_REQUEST",
                message: "The content request is invalid.");

        public static readonly Error InvalidSortField =
            Error.Validation(
                code: "CONTENT.INVALID_SORT_FIELD",
                message: "The requested sort field is not supported.");

        public static readonly Error InvalidStateTransition =
            Error.Conflict(
                code: "CONTENT.INVALID_STATE_TRANSITION",
                message: "The requested article state transition is not allowed.");

        public static readonly Error UnpublishReasonRequired =
            Error.Validation(
                code: "CONTENT.UNPUBLISH_REASON_REQUIRED",
                message: "Unpublish reason is required.");

        public static readonly Error TaxonomyOrphanReference =
            Error.Conflict(
                code: "CONTENT.TAXONOMY_ORPHAN_REFERENCE",
                message: "The requested category or tag reference does not exist.");

        public static readonly Error ConcurrencyConflict =
            Error.Conflict(
                code: "CONTENT.CONCURRENCY_CONFLICT",
                message: "The content resource was modified by another operation. Please reload and try again.");

        public static readonly Error PolicyDenied =
            Error.Forbidden(
                code: "CONTENT.POLICY_DENIED",
                message: "The content action is not allowed.");

        public static readonly Error WriteCommitFailed =
            Error.Failure(
                code: "CONTENT.WRITE_COMMIT_FAILED",
                message: "Content write could not be completed.");

        public static readonly Error OutboxIntentCommitFailed =
            Error.Failure(
                code: "CONTENT.OUTBOX_INTENT_COMMIT_FAILED",
                message: "Content outbox intent could not be committed.");

        public static class Article
        {
            public static readonly Error NotFound =
                Error.NotFound(
                    code: "CONTENT.ARTICLE_NOT_FOUND",
                    message: "Article was not found.");

            public static readonly Error InvalidArticleId =
                Error.Validation(
                    code: "CONTENT.ARTICLE_INVALID_ARTICLE_ID",
                    message: "Article id must be greater than zero.");

            public static readonly Error PublicIdRequired =
                Error.Validation(
                    code: "CONTENT.ARTICLE_PUBLIC_ID_REQUIRED",
                    message: "Article public id is required.");

            public static readonly Error PublicIdInvalid =
                Error.Validation(
                    code: "CONTENT.ARTICLE_PUBLIC_ID_INVALID",
                    message: "Article public id must be a 26-character ULID.");

            public static readonly Error PublicIdAlreadyExists =
                Error.Conflict(
                    code: "CONTENT.ARTICLE_PUBLIC_ID_ALREADY_EXISTS",
                    message: "Article public id already exists.");

            public static readonly Error CategoryIdInvalid =
                Error.Validation(
                    code: "CONTENT.ARTICLE_CATEGORY_ID_INVALID",
                    message: "Category id must be greater than zero.");

            public static readonly Error AuthorUserIdInvalid =
                Error.Validation(
                    code: "CONTENT.ARTICLE_AUTHOR_USER_ID_INVALID",
                    message: "Author user id must be greater than zero.");

            public static readonly Error CreatedByUserIdInvalid =
                Error.Validation(
                    code: "CONTENT.ARTICLE_CREATED_BY_USER_ID_INVALID",
                    message: "Created by user id must be greater than zero.");

            public static readonly Error ActorUserIdInvalid =
                Error.Validation(
                    code: "CONTENT.ARTICLE_ACTOR_USER_ID_INVALID",
                    message: "Actor user id must be greater than zero.");

            public static readonly Error TitleRequired =
                Error.Validation(
                    code: "CONTENT.ARTICLE_TITLE_REQUIRED",
                    message: "Article title is required.");

            public static readonly Error TitleTooLong =
                Error.Validation(
                    code: "CONTENT.ARTICLE_TITLE_TOO_LONG",
                    message: "Article title must not exceed 300 characters.");

            public static readonly Error SummaryRequired =
                Error.Validation(
                    code: "CONTENT.ARTICLE_SUMMARY_REQUIRED",
                    message: "Article summary is required.");

            public static readonly Error SummaryTooLong =
                Error.Validation(
                    code: "CONTENT.ARTICLE_SUMMARY_TOO_LONG",
                    message: "Article summary must not exceed 1000 characters.");

            public static readonly Error BodyRequired =
                Error.Validation(
                    code: "CONTENT.ARTICLE_BODY_REQUIRED",
                    message: "Article body is required.");

            public static readonly Error BodyTooLong =
                Error.Validation(
                    code: "CONTENT.ARTICLE_BODY_TOO_LONG",
                    message: "Article body is too long.");

            public static readonly Error InvalidVersion =
                Error.Validation(
                    code: "CONTENT.ARTICLE_INVALID_VERSION",
                    message: "Article version must be greater than zero.");

            public static readonly Error NotPublishable =
                Error.Conflict(
                    code: "CONTENT.ARTICLE_NOT_PUBLISHABLE",
                    message: "Article cannot be published in its current state.");

            public static readonly Error AlreadyPublished =
                Error.Conflict(
                    code: "CONTENT.ARTICLE_ALREADY_PUBLISHED",
                    message: "Article is already published.");

            public static readonly Error NotPublished =
                Error.Conflict(
                    code: "CONTENT.ARTICLE_NOT_PUBLISHED",
                    message: "Article is not currently published.");

            public static readonly Error AlreadyDraft =
                Error.Conflict(
                    code: "CONTENT.ARTICLE_ALREADY_DRAFT",
                    message: "Article is already in draft state.");

            public static readonly Error AlreadyArchived =
                Error.Conflict(
                    code: "CONTENT.ARTICLE_ALREADY_ARCHIVED",
                    message: "Article is already archived.");

            public static readonly Error NotArchived =
                Error.Conflict(
                    code: "CONTENT.ARTICLE_NOT_ARCHIVED",
                    message: "Article is not archived.");

            public static readonly Error AlreadyDeleted =
                Error.Conflict(
                    code: "CONTENT.ARTICLE_ALREADY_DELETED",
                    message: "Article is already deleted.");

            public static readonly Error AlreadySoftDeleted =
                Error.Conflict(
                    code: "CONTENT.ARTICLE_ALREADY_SOFT_DELETED",
                    message: "Article is already soft-deleted.");

            public static readonly Error NotDraft =
                Error.Conflict(
                    code: "CONTENT.ARTICLE_NOT_DRAFT",
                    message: "Article is not in draft state.");

            public static readonly Error CategoryInactiveOrDeleted =
                Error.Conflict(
                    code: "CONTENT.ARTICLE_CATEGORY_INACTIVE_OR_DELETED",
                    message: "Article category is deleted or inactive.");
        }

        public static class Category
        {
            public static readonly Error NotFound =
                Error.NotFound(
                    code: "CONTENT.CATEGORY_NOT_FOUND",
                    message: "Category was not found.");

            public static readonly Error Conflict =
                Error.Conflict(
                    code: "CONTENT.CATEGORY_CONFLICT",
                    message: "Category already exists or violates a content taxonomy rule.");

            public static readonly Error InvalidCategoryId =
                Error.Validation(
                    code: "CONTENT.CATEGORY_INVALID_CATEGORY_ID",
                    message: "Category id must be greater than zero.");

            public static readonly Error PublicIdRequired =
                Error.Validation(
                    code: "CONTENT.CATEGORY_PUBLIC_ID_REQUIRED",
                    message: "Category public id is required.");

            public static readonly Error PublicIdInvalid =
                Error.Validation(
                    code: "CONTENT.CATEGORY_PUBLIC_ID_INVALID",
                    message: "Category public id must be exactly 26 characters.");

            public static readonly Error PublicIdAlreadyExists =
                Error.Conflict(
                    code: "CONTENT.CATEGORY_PUBLIC_ID_ALREADY_EXISTS",
                    message: "Category public id already exists.");

            public static readonly Error NameRequired =
                Error.Validation(
                    code: "CONTENT.CATEGORY_NAME_REQUIRED",
                    message: "Category name is required.");

            public static readonly Error NameTooLong =
                Error.Validation(
                    code: "CONTENT.CATEGORY_NAME_TOO_LONG",
                    message: "Category name must not exceed 200 characters.");

            public static readonly Error NameNormalizedRequired =
                Error.Validation(
                    code: "CONTENT.CATEGORY_NAME_NORMALIZED_REQUIRED",
                    message: "Category normalized name is required.");

            public static readonly Error NameNormalizedTooLong =
                Error.Validation(
                    code: "CONTENT.CATEGORY_NAME_NORMALIZED_TOO_LONG",
                    message: "Category normalized name must not exceed 200 characters.");

            public static readonly Error NameNormalizedAlreadyExists =
                Error.Conflict(
                    code: "CONTENT.CATEGORY_NAME_NORMALIZED_ALREADY_EXISTS",
                    message: "Category normalized name already exists.");

            public static readonly Error DisplayOrderInvalid =
                Error.Validation(
                    code: "CONTENT.CATEGORY_DISPLAY_ORDER_INVALID",
                    message: "Category display order must be greater than or equal to zero.");

            public static readonly Error ParentIdInvalid =
                Error.Validation(
                    code: "CONTENT.CATEGORY_PARENT_ID_INVALID",
                    message: "Parent category id must be greater than zero.");

            public static readonly Error ParentNotFound =
                Error.NotFound(
                    code: "CONTENT.CATEGORY_PARENT_NOT_FOUND",
                    message: "Parent category does not exist or was deleted.");

            public static readonly Error ParentSelfReference =
                Error.Conflict(
                    code: "CONTENT.CATEGORY_PARENT_SELF_REFERENCE",
                    message: "Category cannot be its own parent.");

            public static readonly Error CycleDetected =
                Error.Conflict(
                    code: "CONTENT.CATEGORY_CYCLE_DETECTED",
                    message: "Category hierarchy cycle was detected.");

            public static readonly Error AlreadyDeleted =
                Error.Conflict(
                    code: "CONTENT.CATEGORY_ALREADY_DELETED",
                    message: "Category is already deleted.");

            public static readonly Error NotDeleted =
                Error.Conflict(
                    code: "CONTENT.CATEGORY_NOT_DELETED",
                    message: "Category is not deleted.");

            public static readonly Error AlreadyActive =
                Error.Conflict(
                    code: "CONTENT.CATEGORY_ALREADY_ACTIVE",
                    message: "Category is already active.");

            public static readonly Error AlreadyInactive =
                Error.Conflict(
                    code: "CONTENT.CATEGORY_ALREADY_INACTIVE",
                    message: "Category is already inactive.");

            public static readonly Error InvalidVersion =
                Error.Validation(
                    code: "CONTENT.CATEGORY_INVALID_VERSION",
                    message: "Category version must be greater than zero.");

            public static readonly Error DeleteBlockedByArticles =
                Error.Conflict(
                    code: "CONTENT.CATEGORY_DELETE_BLOCKED_BY_ARTICLES",
                    message: "Category cannot be deleted because active articles still reference it.");

            public static readonly Error InactiveOrDeleted =
                Error.Conflict(
                    code: "CONTENT.CATEGORY_INACTIVE_OR_DELETED",
                    message: "Category is deleted or inactive.");
        }

        public static class Tag
        {
            public static readonly Error NotFound =
                Error.NotFound(
                    code: "CONTENT.TAG_NOT_FOUND",
                    message: "Tag was not found.");

            public static readonly Error Conflict =
                Error.Conflict(
                    code: "CONTENT.TAG_CONFLICT",
                    message: "Tag already exists or violates a content taxonomy rule.");

            public static readonly Error InvalidTagId =
                Error.Validation(
                    code: "CONTENT.TAG_INVALID_TAG_ID",
                    message: "Tag id must be greater than zero.");

            public static readonly Error PublicIdRequired =
                Error.Validation(
                    code: "CONTENT.TAG_PUBLIC_ID_REQUIRED",
                    message: "Tag public id is required.");

            public static readonly Error PublicIdInvalid =
                Error.Validation(
                    code: "CONTENT.TAG_PUBLIC_ID_INVALID",
                    message: "Tag public id must be exactly 26 characters.");

            public static readonly Error PublicIdAlreadyExists =
                Error.Conflict(
                    code: "CONTENT.TAG_PUBLIC_ID_ALREADY_EXISTS",
                    message: "Tag public id already exists.");

            public static readonly Error NameRequired =
                Error.Validation(
                    code: "CONTENT.TAG_NAME_REQUIRED",
                    message: "Tag name is required.");

            public static readonly Error NameTooLong =
                Error.Validation(
                    code: "CONTENT.TAG_NAME_TOO_LONG",
                    message: "Tag name must not exceed 150 characters.");

            public static readonly Error NameNormalizedRequired =
                Error.Validation(
                    code: "CONTENT.TAG_NAME_NORMALIZED_REQUIRED",
                    message: "Tag normalized name is required.");

            public static readonly Error NameNormalizedTooLong =
                Error.Validation(
                    code: "CONTENT.TAG_NAME_NORMALIZED_TOO_LONG",
                    message: "Tag normalized name must not exceed 150 characters.");

            public static readonly Error NameNormalizedAlreadyExists =
                Error.Conflict(
                    code: "CONTENT.TAG_NAME_NORMALIZED_ALREADY_EXISTS",
                    message: "Tag normalized name already exists.");

            public static readonly Error DescriptionTooLong =
                Error.Validation(
                    code: "CONTENT.TAG_DESCRIPTION_TOO_LONG",
                    message: "Tag description must not exceed 500 characters.");

            public static readonly Error AlreadyDeleted =
                Error.Conflict(
                    code: "CONTENT.TAG_ALREADY_DELETED",
                    message: "Tag is already deleted.");

            public static readonly Error NotDeleted =
                Error.Conflict(
                    code: "CONTENT.TAG_NOT_DELETED",
                    message: "Tag is not deleted.");

            public static readonly Error AlreadyActive =
                Error.Conflict(
                    code: "CONTENT.TAG_ALREADY_ACTIVE",
                    message: "Tag is already active.");

            public static readonly Error AlreadyInactive =
                Error.Conflict(
                    code: "CONTENT.TAG_ALREADY_INACTIVE",
                    message: "Tag is already inactive.");

            public static readonly Error InvalidVersion =
                Error.Validation(
                    code: "CONTENT.TAG_INVALID_VERSION",
                    message: "Tag version must be greater than zero.");

            public static readonly Error InvalidUpdatedAt =
                Error.Validation(
                    code: "CONTENT.TAG_INVALID_UPDATED_AT",
                    message: "Tag updated time cannot be earlier than created time.");

            public static readonly Error InvalidDeletedAt =
                Error.Validation(
                    code: "CONTENT.TAG_INVALID_DELETED_AT",
                    message: "Tag deleted time cannot be earlier than created time.");

            public static readonly Error NotAttachable =
                Error.Conflict(
                    code: "CONTENT.TAG_NOT_ATTACHABLE",
                    message: "Tag is deleted or inactive and cannot be attached to article.");
        }

        public static class ArticleTag
        {
            public static readonly Error AlreadyExists =
                Error.Conflict(
                    code: "CONTENT.ARTICLE_TAG_ALREADY_EXISTS",
                    message: "Article tag attachment already exists.");

            public static readonly Error NotFound =
                Error.NotFound(
                    code: "CONTENT.ARTICLE_TAG_NOT_FOUND",
                    message: "Article tag attachment was not found.");

            public static readonly Error InvalidArticleId =
                Error.Validation(
                    code: "CONTENT.ARTICLE_TAG_INVALID_ARTICLE_ID",
                    message: "Article id must be greater than zero.");

            public static readonly Error InvalidTagId =
                Error.Validation(
                    code: "CONTENT.ARTICLE_TAG_INVALID_TAG_ID",
                    message: "Tag id must be greater than zero.");

            public static readonly Error ArticleNotDraft =
                Error.Conflict(
                    code: "CONTENT.ARTICLE_TAG_ARTICLE_NOT_DRAFT",
                    message: "Tags can only be changed while article is in draft state.");

            public static readonly Error TagNotAttachable =
                Error.Conflict(
                    code: "CONTENT.ARTICLE_TAG_TAG_NOT_ATTACHABLE",
                    message: "Tag is deleted or inactive and cannot be attached to article.");
        }

        public static class Revision
        {
            public static readonly Error NotFound =
                Error.NotFound(
                    code: "CONTENT.ARTICLE_REVISION_NOT_FOUND",
                    message: "Article revision was not found.");

            public static readonly Error InvalidRevisionId =
                Error.Validation(
                    code: "CONTENT.ARTICLE_REVISION_INVALID_REVISION_ID",
                    message: "Revision id must be greater than zero.");

            public static readonly Error InvalidArticleId =
                Error.Validation(
                    code: "CONTENT.ARTICLE_REVISION_INVALID_ARTICLE_ID",
                    message: "Article id must be greater than zero.");

            public static readonly Error InvalidEditorUserId =
                Error.Validation(
                    code: "CONTENT.ARTICLE_REVISION_INVALID_EDITOR_USER_ID",
                    message: "Edited by user id must be greater than zero.");

            public static readonly Error InvalidArticleVersion =
                Error.Validation(
                    code: "CONTENT.ARTICLE_REVISION_INVALID_ARTICLE_VERSION",
                    message: "Article version must be greater than zero when provided.");

            public static readonly Error PreviousSnapshotRequired =
                Error.Validation(
                    code: "CONTENT.ARTICLE_REVISION_PREVIOUS_SNAPSHOT_REQUIRED",
                    message: "Article revision requires at least one previous value.");

            public static readonly Error ChangeSummaryTooLong =
                Error.Validation(
                    code: "CONTENT.ARTICLE_REVISION_CHANGE_SUMMARY_TOO_LONG",
                    message: "Change summary must not exceed 300 characters.");

            public static readonly Error CorrelationIdTooLong =
                Error.Validation(
                    code: "CONTENT.ARTICLE_REVISION_CORRELATION_ID_TOO_LONG",
                    message: "Correlation id must not exceed 100 characters.");
        }

        public static class LifecycleEvent
        {
            public static readonly Error NotFound =
                Error.NotFound(
                    code: "CONTENT.ARTICLE_LIFECYCLE_EVENT_NOT_FOUND",
                    message: "Article lifecycle event was not found.");

            public static readonly Error InvalidEventId =
                Error.Validation(
                    code: "CONTENT.ARTICLE_LIFECYCLE_EVENT_INVALID_EVENT_ID",
                    message: "Lifecycle event id must be greater than zero.");

            public static readonly Error InvalidArticleId =
                Error.Validation(
                    code: "CONTENT.ARTICLE_LIFECYCLE_EVENT_INVALID_ARTICLE_ID",
                    message: "Article id must be greater than zero.");

            public static readonly Error InvalidArticleVersion =
                Error.Validation(
                    code: "CONTENT.ARTICLE_LIFECYCLE_EVENT_INVALID_ARTICLE_VERSION",
                    message: "Article version must be greater than zero.");

            public static readonly Error ActionTypeRequired =
                Error.Validation(
                    code: "CONTENT.ARTICLE_LIFECYCLE_EVENT_ACTION_TYPE_REQUIRED",
                    message: "Lifecycle action type is required.");

            public static readonly Error InvalidActionType =
                Error.Validation(
                    code: "CONTENT.ARTICLE_LIFECYCLE_EVENT_ACTION_TYPE_INVALID",
                    message: "Lifecycle action type is invalid.");

            public static readonly Error InvalidStatus =
                Error.Validation(
                    code: "CONTENT.ARTICLE_LIFECYCLE_EVENT_STATUS_INVALID",
                    message: "Lifecycle event status is invalid.");

            public static readonly Error ReasonTooLong =
                Error.Validation(
                    code: "CONTENT.ARTICLE_LIFECYCLE_EVENT_REASON_TOO_LONG",
                    message: "Lifecycle event reason must not exceed 500 characters.");

            public static readonly Error UnpublishReasonRequired =
                Error.Validation(
                    code: "CONTENT.ARTICLE_LIFECYCLE_EVENT_UNPUBLISH_REASON_REQUIRED",
                    message: "Reason is required for unpublish lifecycle event.");

            public static readonly Error InvalidActorUserId =
                Error.Validation(
                    code: "CONTENT.ARTICLE_LIFECYCLE_EVENT_INVALID_ACTOR_USER_ID",
                    message: "Actor user id must be greater than zero.");

            public static readonly Error CorrelationIdTooLong =
                Error.Validation(
                    code: "CONTENT.ARTICLE_LIFECYCLE_EVENT_CORRELATION_ID_TOO_LONG",
                    message: "Correlation id must not exceed 100 characters.");
        }
    }
}