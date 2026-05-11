using Content.Domain.Exceptions;

namespace Content.Domain.Entities
{
    public sealed class ArticleRevision
    {
        private const int CorrelationIdMaxLength = 100;
        private const int ChangeSummaryMaxLength = 300;
        private const int TitleMaxLength = 300;
        private const int SummaryMaxLength = 1000;

        private ArticleRevision(
            long revisionId,
            long articleId,
            DateTime editedAt,
            long editedByUserId,
            long? articleVersion,
            string? correlationId,
            string? changeSummary,
            string? oldTitle,
            string? oldSummary,
            string? oldBody)
        {
            RevisionId = revisionId;
            ArticleId = articleId;
            EditedAt = editedAt;
            EditedByUserId = editedByUserId;
            ArticleVersion = articleVersion;
            CorrelationId = correlationId;
            ChangeSummary = changeSummary;
            OldTitle = oldTitle;
            OldSummary = oldSummary;
            OldBody = oldBody;
        }

        public long RevisionId { get; private set; }

        public long ArticleId { get; private set; }

        public DateTime EditedAt { get; private set; }

        public long EditedByUserId { get; private set; }

        public long? ArticleVersion { get; private set; }

        public string? CorrelationId { get; private set; }

        public string? ChangeSummary { get; private set; }

        public string? OldTitle { get; private set; }

        public string? OldSummary { get; private set; }

        public string? OldBody { get; private set; }

        public bool HasPreviousSnapshot =>
            OldTitle is not null ||
            OldSummary is not null ||
            OldBody is not null;

        public static ArticleRevision Create(
            long articleId,
            long editedByUserId,
            long? articleVersion,
            string? correlationId,
            string? changeSummary,
            string? oldTitle,
            string? oldSummary,
            string? oldBody,
            DateTime nowUtc)
        {
            ValidateArticleId(articleId);
            ValidateEditedByUserId(editedByUserId);
            ValidateArticleVersion(articleVersion);
            ValidateCorrelationId(correlationId);
            ValidateChangeSummary(changeSummary);
            ValidateOldTitle(oldTitle);
            ValidateOldSummary(oldSummary);
            ValidatePreviousSnapshot(oldTitle, oldSummary, oldBody);

            return new ArticleRevision(
                revisionId: 0,
                articleId: articleId,
                editedAt: nowUtc,
                editedByUserId: editedByUserId,
                articleVersion: articleVersion,
                correlationId: NormalizeOptional(correlationId),
                changeSummary: NormalizeOptional(changeSummary),
                oldTitle: NormalizeOptional(oldTitle),
                oldSummary: NormalizeOptional(oldSummary),
                oldBody: NormalizeOptional(oldBody));
        }

        public static ArticleRevision Rehydrate(
            long revisionId,
            long articleId,
            DateTime editedAt,
            long editedByUserId,
            long? articleVersion,
            string? correlationId,
            string? changeSummary,
            string? oldTitle,
            string? oldSummary,
            string? oldBody)
        {
            if (revisionId <= 0)
            {
                throw new ContentDomainException(
                    "CONTENT.ARTICLE_REVISION_INVALID_REVISION_ID",
                    "Revision id must be greater than zero.");
            }

            ValidateArticleId(articleId);
            ValidateEditedByUserId(editedByUserId);
            ValidateArticleVersion(articleVersion);
            ValidateCorrelationId(correlationId);
            ValidateChangeSummary(changeSummary);
            ValidateOldTitle(oldTitle);
            ValidateOldSummary(oldSummary);
            ValidatePreviousSnapshot(oldTitle, oldSummary, oldBody);

            return new ArticleRevision(
                revisionId: revisionId,
                articleId: articleId,
                editedAt: editedAt,
                editedByUserId: editedByUserId,
                articleVersion: articleVersion,
                correlationId: NormalizeOptional(correlationId),
                changeSummary: NormalizeOptional(changeSummary),
                oldTitle: NormalizeOptional(oldTitle),
                oldSummary: NormalizeOptional(oldSummary),
                oldBody: NormalizeOptional(oldBody));
        }

        private static void ValidateArticleId(long articleId)
        {
            if (articleId <= 0)
            {
                throw new ContentDomainException(
                    "CONTENT.ARTICLE_REVISION_INVALID_ARTICLE_ID",
                    "Article id must be greater than zero.");
            }
        }

        private static void ValidateEditedByUserId(long editedByUserId)
        {
            if (editedByUserId <= 0)
            {
                throw new ContentDomainException(
                    "CONTENT.ARTICLE_REVISION_INVALID_EDITOR_USER_ID",
                    "Edited by user id must be greater than zero.");
            }
        }

        private static void ValidateArticleVersion(long? articleVersion)
        {
            if (articleVersion.HasValue && articleVersion.Value <= 0)
            {
                throw new ContentDomainException(
                    "CONTENT.ARTICLE_REVISION_INVALID_ARTICLE_VERSION",
                    "Article version must be greater than zero when provided.");
            }
        }

        private static void ValidateCorrelationId(string? correlationId)
        {
            if (correlationId is not null &&
                correlationId.Trim().Length > CorrelationIdMaxLength)
            {
                throw new ContentDomainException(
                    "CONTENT.ARTICLE_REVISION_CORRELATION_ID_TOO_LONG",
                    $"Correlation id must not exceed {CorrelationIdMaxLength} characters.");
            }
        }

        private static void ValidateChangeSummary(string? changeSummary)
        {
            if (changeSummary is not null &&
                changeSummary.Trim().Length > ChangeSummaryMaxLength)
            {
                throw new ContentDomainException(
                    "CONTENT.ARTICLE_REVISION_CHANGE_SUMMARY_TOO_LONG",
                    $"Change summary must not exceed {ChangeSummaryMaxLength} characters.");
            }
        }

        private static void ValidateOldTitle(string? oldTitle)
        {
            if (oldTitle is not null &&
                oldTitle.Trim().Length > TitleMaxLength)
            {
                throw new ContentDomainException(
                    "CONTENT.ARTICLE_REVISION_OLD_TITLE_TOO_LONG",
                    $"Old title must not exceed {TitleMaxLength} characters.");
            }
        }

        private static void ValidateOldSummary(string? oldSummary)
        {
            if (oldSummary is not null &&
                oldSummary.Trim().Length > SummaryMaxLength)
            {
                throw new ContentDomainException(
                    "CONTENT.ARTICLE_REVISION_OLD_SUMMARY_TOO_LONG",
                    $"Old summary must not exceed {SummaryMaxLength} characters.");
            }
        }

        private static void ValidatePreviousSnapshot(
            string? oldTitle,
            string? oldSummary,
            string? oldBody)
        {
            if (oldTitle is null &&
                oldSummary is null &&
                oldBody is null)
            {
                throw new ContentDomainException(
                    "CONTENT.ARTICLE_REVISION_PREVIOUS_SNAPSHOT_REQUIRED",
                    "Article revision requires at least one previous value.");
            }
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}