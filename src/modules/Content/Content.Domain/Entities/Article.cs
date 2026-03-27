using Content.Domain.Enums;
using Content.Domain.Exceptions;

namespace Content.Domain.Entities
{
    public sealed class Article
    {
        public long ArticleId { get; private set; }
        public string PublicId { get; private set; } = string.Empty;

        public string Title { get; private set; } = string.Empty;
        public string? Summary { get; private set; }
        public string Body { get; private set; } = string.Empty;

        public string Status { get; private set; } = ArticleStatus.Draft;

        public long AuthorUserId { get; private set; }
        public long? CategoryId { get; private set; }
        public long? CoverMediaId { get; private set; }

        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        public DateTime? PublishedAt { get; private set; }
        public DateTime? UnpublishedAt { get; private set; }
        public DateTime? ArchivedAt { get; private set; }

        public long? CreatedByUserId { get; private set; }
        public long? UpdatedByUserId { get; private set; }

        public bool IsDeleted { get; private set; }
        public DateTime? DeletedAt { get; private set; }
        public long? DeletedByUserId { get; private set; }

        public int Version { get; private set; }

        private Article()
        {
        }

        public static Article CreateDraft(
            string publicId,
            long authorUserId,
            string title,
            string body,
            string? summary,
            long? categoryId,
            long? coverMediaId,
            DateTime nowUtc,
            long? actorUserId)
        {
            ValidatePublicId(publicId);
            ValidateAuthorUserId(authorUserId);
            ValidateTitle(title);
            ValidateBody(body);

            return new Article
            {
                PublicId = publicId.Trim(),
                AuthorUserId = authorUserId,
                Title = title.Trim(),
                Summary = NormalizeOptional(summary),
                Body = body.Trim(),
                CategoryId = categoryId,
                CoverMediaId = coverMediaId,
                Status = ArticleStatus.Draft,
                CreatedAt = nowUtc,
                UpdatedAt = nowUtc,
                CreatedByUserId = actorUserId,
                UpdatedByUserId = actorUserId,
                IsDeleted = false,
                Version = 1
            };
        }

        public static Article Rehydrate(
            long articleId,
            string publicId,
            string title,
            string? summary,
            string body,
            string status,
            long authorUserId,
            long? categoryId,
            long? coverMediaId,
            DateTime createdAt,
            DateTime updatedAt,
            DateTime? publishedAt,
            DateTime? unpublishedAt,
            DateTime? archivedAt,
            long? createdByUserId,
            long? updatedByUserId,
            bool isDeleted,
            DateTime? deletedAt,
            long? deletedByUserId,
            int version)
        {
            if (articleId <= 0)
            {
                throw new ContentDomainException(
                    "CONTENT.ARTICLE_INVALID_ARTICLE_ID",
                    "Article id must be greater than zero.");
            }

            if (!ArticleStatus.IsValid(status))
            {
                throw new ContentDomainException(
                    "CONTENT.ARTICLE_INVALID_STATUS",
                    "Article status is invalid.");
            }

            if (version <= 0)
            {
                throw new ContentDomainException(
                    "CONTENT.ARTICLE_INVALID_VERSION",
                    "Article version must be greater than zero.");
            }

            ValidatePublicId(publicId);
            ValidateAuthorUserId(authorUserId);
            ValidateTitle(title);
            ValidateBody(body);

            return new Article
            {
                ArticleId = articleId,
                PublicId = publicId.Trim(),
                Title = title.Trim(),
                Summary = NormalizeOptional(summary),
                Body = body.Trim(),
                Status = status,
                AuthorUserId = authorUserId,
                CategoryId = categoryId,
                CoverMediaId = coverMediaId,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt,
                PublishedAt = publishedAt,
                UnpublishedAt = unpublishedAt,
                ArchivedAt = archivedAt,
                CreatedByUserId = createdByUserId,
                UpdatedByUserId = updatedByUserId,
                IsDeleted = isDeleted,
                DeletedAt = deletedAt,
                DeletedByUserId = deletedByUserId,
                Version = version
            };
        }

        public void UpdateDraft(
            string title,
            string body,
            string? summary,
            long? categoryId,
            long? coverMediaId,
            DateTime nowUtc,
            long? actorUserId)
        {
            EnsureNotDeleted();

            if (Status != ArticleStatus.Draft)
            {
                throw new ContentDomainException(
                    "CONTENT.INVALID_STATE_TRANSITION",
                    "Only draft articles can be updated through the draft update flow.");
            }

            ValidateTitle(title);
            ValidateBody(body);

            Title = title.Trim();
            Body = body.Trim();
            Summary = NormalizeOptional(summary);
            CategoryId = categoryId;
            CoverMediaId = coverMediaId;
            UpdatedAt = nowUtc;
            UpdatedByUserId = actorUserId;
            Version++;
        }

        public void Publish(DateTime nowUtc, long? actorUserId)
        {
            EnsureNotDeleted();

            if (Status == ArticleStatus.Published)
            {
                throw new ContentDomainException(
                    "CONTENT.ARTICLE_ALREADY_PUBLISHED",
                    "Article is already published.");
            }

            if (Status == ArticleStatus.Archived)
            {
                throw new ContentDomainException(
                    "CONTENT.INVALID_STATE_TRANSITION",
                    "Archived articles must be restored to draft before publishing.");
            }

            ValidateTitle(Title);
            ValidateBody(Body);

            Status = ArticleStatus.Published;
            PublishedAt ??= nowUtc;
            UpdatedAt = nowUtc;
            UpdatedByUserId = actorUserId;
            Version++;
        }

        public void Unpublish(string reason, DateTime nowUtc, long? actorUserId)
        {
            EnsureNotDeleted();

            if (Status != ArticleStatus.Published)
            {
                throw new ContentDomainException(
                    "CONTENT.ARTICLE_NOT_PUBLISHED",
                    "Article is not currently published.");
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ContentDomainException(
                    "CONTENT.UNPUBLISH_REASON_REQUIRED",
                    "Unpublish reason is required.");
            }

            Status = ArticleStatus.Draft;
            UnpublishedAt = nowUtc;
            UpdatedAt = nowUtc;
            UpdatedByUserId = actorUserId;
            Version++;
        }

        public void Archive(DateTime nowUtc, long? actorUserId)
        {
            EnsureNotDeleted();

            if (Status == ArticleStatus.Archived)
            {
                throw new ContentDomainException(
                    "CONTENT.ARTICLE_ALREADY_ARCHIVED",
                    "Article is already archived.");
            }

            Status = ArticleStatus.Archived;
            ArchivedAt = nowUtc;
            UpdatedAt = nowUtc;
            UpdatedByUserId = actorUserId;
            Version++;
        }

        public void RestoreToDraft(DateTime nowUtc, long? actorUserId)
        {
            EnsureNotDeleted();

            if (Status != ArticleStatus.Archived)
            {
                throw new ContentDomainException(
                    "CONTENT.ARTICLE_NOT_ARCHIVED",
                    "Article is not archived.");
            }

            Status = ArticleStatus.Draft;
            ArchivedAt = null;
            UpdatedAt = nowUtc;
            UpdatedByUserId = actorUserId;
            Version++;
        }

        public void SoftDelete(DateTime nowUtc, long? actorUserId)
        {
            if (IsDeleted)
            {
                throw new ContentDomainException(
                    "CONTENT.ARTICLE_ALREADY_DELETED",
                    "Article is already deleted.");
            }

            IsDeleted = true;
            DeletedAt = nowUtc;
            DeletedByUserId = actorUserId;
            UpdatedAt = nowUtc;
            UpdatedByUserId = actorUserId;
            Version++;
        }

        private void EnsureNotDeleted()
        {
            if (IsDeleted)
            {
                throw new ContentDomainException(
                    "CONTENT.ARTICLE_ALREADY_DELETED",
                    "Article is already deleted.");
            }
        }

        private static void ValidatePublicId(string publicId)
        {
            if (string.IsNullOrWhiteSpace(publicId))
            {
                throw new ContentDomainException(
                    "CONTENT.ARTICLE_PUBLIC_ID_REQUIRED",
                    "Article public id is required.");
            }
        }

        private static void ValidateAuthorUserId(long authorUserId)
        {
            if (authorUserId <= 0)
            {
                throw new ContentDomainException(
                    "CONTENT.ARTICLE_AUTHOR_USER_ID_INVALID",
                    "Author user id must be greater than zero.");
            }
        }

        private static void ValidateTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ContentDomainException(
                    "CONTENT.ARTICLE_TITLE_REQUIRED",
                    "Article title is required.");
            }

            if (title.Trim().Length > 300)
            {
                throw new ContentDomainException(
                    "CONTENT.ARTICLE_TITLE_TOO_LONG",
                    "Article title must not exceed 300 characters.");
            }
        }

        private static void ValidateBody(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                throw new ContentDomainException(
                    "CONTENT.ARTICLE_BODY_REQUIRED",
                    "Article body is required.");
            }
        }

        private static string? NormalizeOptional(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim();
        }
    }
}