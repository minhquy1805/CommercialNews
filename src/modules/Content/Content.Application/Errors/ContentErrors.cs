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

