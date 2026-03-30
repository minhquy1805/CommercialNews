using CommercialNews.BuildingBlocks.Results;

namespace Content.Application.Errors
{
    public static class ContentErrors
    {
        public static readonly Error ValidationFailed =
            Error.Validation(
                code: "CONTENT.VALIDATION_FAILED",
                message: "One or more content validations failed.");

        public static readonly Error InvalidStateTransition =
            Error.Validation(
                code: "CONTENT.INVALID_STATE_TRANSITION",
                message: "The requested article state transition is not allowed.");

        public static readonly Error UnpublishReasonRequired =
            Error.Validation(
                code: "CONTENT.UNPUBLISH_REASON_REQUIRED",
                message: "Unpublish reason is required.");

        public static readonly Error TaxonomyOrphanReference =
            Error.Validation(
                code: "CONTENT.TAXONOMY_ORPHAN_REFERENCE",
                message: "The requested category or tag reference does not exist.");

        public static readonly Error ConcurrencyConflict =
            Error.Conflict(
                code: "CONTENT.CONCURRENCY_CONFLICT",
                message: "The article was modified by another operation. Please reload and try again.");

        public static class Article
        {
            public static readonly Error NotFound =
                Error.NotFound(
                    code: "CONTENT.ARTICLE_NOT_FOUND",
                    message: "Article was not found.");

            public static readonly Error TitleRequired =
                Error.Validation(
                    code: "CONTENT.ARTICLE_TITLE_REQUIRED",
                    message: "Article title is required.");

            public static readonly Error TitleTooLong =
                Error.Validation(
                    code: "CONTENT.ARTICLE_TITLE_TOO_LONG",
                    message: "Article title must not exceed 300 characters.");

            public static readonly Error BodyRequired =
                Error.Validation(
                    code: "CONTENT.ARTICLE_BODY_REQUIRED",
                    message: "Article body is required.");

            public static readonly Error PublicIdRequired =
                Error.Validation(
                    code: "CONTENT.ARTICLE_PUBLIC_ID_REQUIRED",
                    message: "Article public id is required.");

            public static readonly Error AuthorUserIdInvalid =
                Error.Validation(
                    code: "CONTENT.ARTICLE_AUTHOR_USER_ID_INVALID",
                    message: "Author user id must be greater than zero.");

            public static readonly Error AlreadyPublished =
                Error.Validation(
                    code: "CONTENT.ARTICLE_ALREADY_PUBLISHED",
                    message: "Article is already published.");

            public static readonly Error NotPublished =
                Error.Validation(
                    code: "CONTENT.ARTICLE_NOT_PUBLISHED",
                    message: "Article is not currently published.");

            public static readonly Error AlreadyArchived =
                Error.Validation(
                    code: "CONTENT.ARTICLE_ALREADY_ARCHIVED",
                    message: "Article is already archived.");

            public static readonly Error NotArchived =
                Error.Validation(
                    code: "CONTENT.ARTICLE_NOT_ARCHIVED",
                    message: "Article is not archived.");

            public static readonly Error AlreadyDeleted =
                Error.Validation(
                    code: "CONTENT.ARTICLE_ALREADY_DELETED",
                    message: "Article is already deleted.");

            public static readonly Error InvalidArticleId =
                Error.Validation(
                    code: "CONTENT.ARTICLE_INVALID_ARTICLE_ID",
                    message: "Article id must be greater than zero.");

            public static readonly Error InvalidVersion =
                Error.Validation(
                    code: "CONTENT.ARTICLE_INVALID_VERSION",
                    message: "Article version must be greater than zero.");
        }

        public static class Category
        {
            public static readonly Error NotFound =
                Error.NotFound(
                    code: "CONTENT.CATEGORY_NOT_FOUND",
                    message: "Category was not found.");

            public static readonly Error PublicIdRequired =
                Error.Validation(
                    code: "CONTENT.CATEGORY_PUBLIC_ID_REQUIRED",
                    message: "Category public id is required.");

            public static readonly Error PublicIdInvalid =
                Error.Validation(
                    code: "CONTENT.CATEGORY_PUBLIC_ID_INVALID",
                    message: "Category public id must be exactly 26 characters.");

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

            public static readonly Error DisplayOrderInvalid =
                Error.Validation(
                    code: "CONTENT.CATEGORY_DISPLAY_ORDER_INVALID",
                    message: "Category display order must be greater than or equal to zero.");

            public static readonly Error ParentSelfReference =
                Error.Validation(
                    code: "CONTENT.CATEGORY_PARENT_SELF_REFERENCE",
                    message: "Category cannot be its own parent.");

            public static readonly Error AlreadyDeleted =
                Error.Validation(
                    code: "CONTENT.CATEGORY_ALREADY_DELETED",
                    message: "Category is already deleted.");

            public static readonly Error NotDeleted =
                Error.Validation(
                    code: "CONTENT.CATEGORY_NOT_DELETED",
                    message: "Category is not deleted.");

            public static readonly Error AlreadyActive =
                Error.Validation(
                    code: "CONTENT.CATEGORY_ALREADY_ACTIVE",
                    message: "Category is already active.");

            public static readonly Error AlreadyInactive =
                Error.Validation(
                    code: "CONTENT.CATEGORY_ALREADY_INACTIVE",
                    message: "Category is already inactive.");

            public static readonly Error InvalidCategoryId =
                Error.Validation(
                    code: "CONTENT.CATEGORY_INVALID_CATEGORY_ID",
                    message: "Category id must be greater than zero.");

            public static readonly Error InvalidVersion =
                Error.Validation(
                    code: "CONTENT.CATEGORY_INVALID_VERSION",
                    message: "Category version must be greater than zero.");

            public static readonly Error ParentNotFound =
                Error.Validation(
                    code: "CONTENT.CATEGORY_PARENT_NOT_FOUND",
                    message: "Parent category does not exist or was deleted.");

            public static readonly Error CycleDetected =
                Error.Validation(
                    code: "CONTENT.CATEGORY_CYCLE_DETECTED",
                    message: "Category hierarchy cycle was detected.");

            public static readonly Error DeleteBlockedByArticles =
                Error.Validation(
                    code: "CONTENT.CATEGORY_DELETE_BLOCKED_BY_ARTICLES",
                    message: "Category cannot be deleted because active articles still reference it.");
        }

        public static class Tag
        {
            public static readonly Error NotFound =
                Error.NotFound(
                    code: "CONTENT.TAG_NOT_FOUND",
                    message: "Tag was not found.");
        }

        public static class Revision
        {
            public static readonly Error NotFound =
                Error.NotFound(
                    code: "CONTENT.ARTICLE_REVISION_NOT_FOUND",
                    message: "Article revision was not found.");

            public static readonly Error InvalidRevisionId =
                Error.Validation(
                    code: "CONTENT.REVISION_INVALID_REVISION_ID",
                    message: "Revision id must be greater than zero.");
        }
    }
}

