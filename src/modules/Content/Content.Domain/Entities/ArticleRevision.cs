using Content.Domain.Common;
using Content.Domain.Constants;
using Content.Domain.Exceptions;

namespace Content.Domain.Entities
{
    public sealed class ArticleRevision
    {
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
            ValidateEditedAt(nowUtc);

            string? normalizedCorrelationId = ContentText.NormalizeOptional(correlationId);
            string? normalizedChangeSummary = ContentText.NormalizeOptional(changeSummary);
            string? normalizedOldTitle = ContentText.NormalizeOptional(oldTitle);
            string? normalizedOldSummary = ContentText.NormalizeOptional(oldSummary);
            string? normalizedOldBody = ContentText.NormalizeOptional(oldBody);

            ValidateCorrelationId(normalizedCorrelationId);
            ValidateChangeSummary(normalizedChangeSummary);
            ValidateOldTitle(normalizedOldTitle);
            ValidateOldSummary(normalizedOldSummary);
            ValidatePreviousSnapshot(
                normalizedOldTitle,
                normalizedOldSummary,
                normalizedOldBody);

            return new ArticleRevision(
                revisionId: 0,
                articleId: articleId,
                editedAt: nowUtc,
                editedByUserId: editedByUserId,
                articleVersion: articleVersion,
                correlationId: normalizedCorrelationId,
                changeSummary: normalizedChangeSummary,
                oldTitle: normalizedOldTitle,
                oldSummary: normalizedOldSummary,
                oldBody: normalizedOldBody);
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
            ContentGuard.AgainstInvalidId(
                revisionId,
                "CONTENT.ARTICLE_REVISION_INVALID_REVISION_ID",
                "Revision id must be greater than zero.");

            ValidateArticleId(articleId);
            ValidateEditedByUserId(editedByUserId);
            ValidateArticleVersion(articleVersion);
            ValidateEditedAt(editedAt);

            string? normalizedCorrelationId = ContentText.NormalizeOptional(correlationId);
            string? normalizedChangeSummary = ContentText.NormalizeOptional(changeSummary);
            string? normalizedOldTitle = ContentText.NormalizeOptional(oldTitle);
            string? normalizedOldSummary = ContentText.NormalizeOptional(oldSummary);
            string? normalizedOldBody = ContentText.NormalizeOptional(oldBody);

            ValidateCorrelationId(normalizedCorrelationId);
            ValidateChangeSummary(normalizedChangeSummary);
            ValidateOldTitle(normalizedOldTitle);
            ValidateOldSummary(normalizedOldSummary);
            ValidatePreviousSnapshot(
                normalizedOldTitle,
                normalizedOldSummary,
                normalizedOldBody);

            return new ArticleRevision(
                revisionId: revisionId,
                articleId: articleId,
                editedAt: editedAt,
                editedByUserId: editedByUserId,
                articleVersion: articleVersion,
                correlationId: normalizedCorrelationId,
                changeSummary: normalizedChangeSummary,
                oldTitle: normalizedOldTitle,
                oldSummary: normalizedOldSummary,
                oldBody: normalizedOldBody);
        }

        private static void ValidateArticleId(long articleId)
        {
            ContentGuard.AgainstInvalidId(
                articleId,
                "CONTENT.ARTICLE_REVISION_INVALID_ARTICLE_ID",
                "Article id must be greater than zero.");
        }

        private static void ValidateEditedByUserId(long editedByUserId)
        {
            ContentGuard.AgainstInvalidId(
                editedByUserId,
                "CONTENT.ARTICLE_REVISION_INVALID_EDITOR_USER_ID",
                "Edited by user id must be greater than zero.");
        }

        private static void ValidateArticleVersion(long? articleVersion)
        {
            ContentGuard.AgainstInvalidOptionalVersion(
                articleVersion,
                "CONTENT.ARTICLE_REVISION_INVALID_ARTICLE_VERSION",
                "Article version must be greater than zero when provided.");
        }

        private static void ValidateEditedAt(DateTime editedAt)
        {
            ContentGuard.AgainstDefaultDateTime(
                editedAt,
                "CONTENT.ARTICLE_REVISION_INVALID_EDITED_AT",
                "Edited time is required.");
        }

        private static void ValidateCorrelationId(string? correlationId)
        {
            ContentGuard.AgainstTooLong(
                correlationId,
                ContentFieldLimits.CorrelationIdMaxLength,
                "CONTENT.ARTICLE_REVISION_CORRELATION_ID_TOO_LONG",
                $"Correlation id must not exceed {ContentFieldLimits.CorrelationIdMaxLength} characters.");
        }

        private static void ValidateChangeSummary(string? changeSummary)
        {
            ContentGuard.AgainstTooLong(
                changeSummary,
                ContentFieldLimits.ChangeSummaryMaxLength,
                "CONTENT.ARTICLE_REVISION_CHANGE_SUMMARY_TOO_LONG",
                $"Change summary must not exceed {ContentFieldLimits.ChangeSummaryMaxLength} characters.");
        }

        private static void ValidateOldTitle(string? oldTitle)
        {
            ContentGuard.AgainstTooLong(
                oldTitle,
                ContentFieldLimits.ArticleTitleMaxLength,
                "CONTENT.ARTICLE_REVISION_OLD_TITLE_TOO_LONG",
                $"Old title must not exceed {ContentFieldLimits.ArticleTitleMaxLength} characters.");
        }

        private static void ValidateOldSummary(string? oldSummary)
        {
            ContentGuard.AgainstTooLong(
                oldSummary,
                ContentFieldLimits.ArticleSummaryMaxLength,
                "CONTENT.ARTICLE_REVISION_OLD_SUMMARY_TOO_LONG",
                $"Old summary must not exceed {ContentFieldLimits.ArticleSummaryMaxLength} characters.");
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
    }
}