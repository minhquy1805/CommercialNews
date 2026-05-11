using Content.Domain.Constants;
using Content.Domain.Exceptions;

namespace Content.Domain.Entities;

public sealed class Article
{
    public long ArticleId { get; private set; }

    public string ArticlePublicId { get; private set; } = string.Empty;

    public long CategoryId { get; private set; }

    public long AuthorUserId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Summary { get; private set; } = string.Empty;

    public string Body { get; private set; } = string.Empty;

    public string Status { get; private set; } = ArticleStatuses.Draft;

    public DateTime? PublishedAt { get; private set; }

    public DateTime? UnpublishedAt { get; private set; }

    public DateTime? ArchivedAt { get; private set; }

    public long? CoverMediaId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public long CreatedByUserId { get; private set; }

    public long? UpdatedByUserId { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAt { get; private set; }

    public long? DeletedByUserId { get; private set; }

    public long Version { get; private set; }

    private Article()
    {
    }

    public bool IsDraft => Status == ArticleStatuses.Draft;

    public bool IsPublished => Status == ArticleStatuses.Published;

    public bool IsArchived => Status == ArticleStatuses.Archived;

    public bool IsPubliclyVisible => Status == ArticleStatuses.Published && !IsDeleted;

    public static Article CreateDraft(
        string articlePublicId,
        long categoryId,
        long authorUserId,
        string title,
        string summary,
        string body,
        long? coverMediaId,
        DateTime nowUtc,
        long actorUserId)
    {
        ValidateArticlePublicId(articlePublicId);
        ValidateCategoryId(categoryId);
        ValidateAuthorUserId(authorUserId);
        ValidateActorUserId(actorUserId);
        ValidateTitle(title);
        ValidateSummary(summary);
        ValidateBody(body);

        return new Article
        {
            ArticlePublicId = articlePublicId.Trim(),
            CategoryId = categoryId,
            AuthorUserId = authorUserId,
            Title = title.Trim(),
            Summary = summary.Trim(),
            Body = body.Trim(),
            Status = ArticleStatuses.Draft,
            PublishedAt = null,
            UnpublishedAt = null,
            ArchivedAt = null,
            CoverMediaId = coverMediaId,
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc,
            CreatedByUserId = actorUserId,
            UpdatedByUserId = actorUserId,
            IsDeleted = false,
            DeletedAt = null,
            DeletedByUserId = null,
            Version = 1
        };
    }

    public static Article Rehydrate(
        long articleId,
        string articlePublicId,
        long categoryId,
        long authorUserId,
        string title,
        string summary,
        string body,
        string status,
        DateTime? publishedAt,
        DateTime? unpublishedAt,
        DateTime? archivedAt,
        long? coverMediaId,
        DateTime createdAt,
        DateTime updatedAt,
        long createdByUserId,
        long? updatedByUserId,
        bool isDeleted,
        DateTime? deletedAt,
        long? deletedByUserId,
        long version)
    {
        if (articleId <= 0)
        {
            throw new ContentDomainException(
                "CONTENT.ARTICLE_INVALID_ARTICLE_ID",
                "Article id must be greater than zero.");
        }

        ValidateArticlePublicId(articlePublicId);
        ValidateCategoryId(categoryId);
        ValidateAuthorUserId(authorUserId);
        ValidateActorUserId(createdByUserId);
        ValidateTitle(title);
        ValidateSummary(summary);
        ValidateBody(body);

        if (!ArticleStatuses.IsValid(status))
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

        return new Article
        {
            ArticleId = articleId,
            ArticlePublicId = articlePublicId.Trim(),
            CategoryId = categoryId,
            AuthorUserId = authorUserId,
            Title = title.Trim(),
            Summary = summary.Trim(),
            Body = body.Trim(),
            Status = status,
            PublishedAt = publishedAt,
            UnpublishedAt = unpublishedAt,
            ArchivedAt = archivedAt,
            CoverMediaId = coverMediaId,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            CreatedByUserId = createdByUserId,
            UpdatedByUserId = updatedByUserId,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt,
            DeletedByUserId = deletedByUserId,
            Version = version
        };
    }

    public bool CanUpdate()
    {
        return !IsDeleted && Status == ArticleStatuses.Draft;
    }

    public bool CanPublish()
    {
        return !IsDeleted && Status == ArticleStatuses.Draft;
    }

    public bool CanUnpublish()
    {
        return !IsDeleted && Status == ArticleStatuses.Published;
    }

    public bool CanArchive()
    {
        return !IsDeleted &&
               (Status == ArticleStatuses.Draft ||
                Status == ArticleStatuses.Published);
    }

    public bool CanSoftDelete()
    {
        return !IsDeleted;
    }

    public void UpdateDraft(
        long categoryId,
        string title,
        string summary,
        string body,
        long? coverMediaId,
        DateTime nowUtc,
        long actorUserId)
    {
        EnsureNotDeleted();

        if (Status != ArticleStatuses.Draft)
        {
            throw new ContentDomainException(
                "CONTENT.INVALID_STATE_TRANSITION",
                "Only draft articles can be updated.");
        }

        ValidateCategoryId(categoryId);
        ValidateTitle(title);
        ValidateSummary(summary);
        ValidateBody(body);
        ValidateActorUserId(actorUserId);

        CategoryId = categoryId;
        Title = title.Trim();
        Summary = summary.Trim();
        Body = body.Trim();
        CoverMediaId = coverMediaId;
        UpdatedAt = nowUtc;
        UpdatedByUserId = actorUserId;
        Version++;
    }

    public void Publish(DateTime nowUtc, long actorUserId)
    {
        EnsureNotDeleted();
        ValidateActorUserId(actorUserId);

        if (Status == ArticleStatuses.Published)
        {
            throw new ContentDomainException(
                "CONTENT.ARTICLE_ALREADY_PUBLISHED",
                "Article is already published.");
        }

        if (Status != ArticleStatuses.Draft)
        {
            throw new ContentDomainException(
                "CONTENT.INVALID_STATE_TRANSITION",
                "Only draft articles can be published.");
        }

        ValidateTitle(Title);
        ValidateSummary(Summary);
        ValidateBody(Body);

        Status = ArticleStatuses.Published;
        PublishedAt = nowUtc;
        ArchivedAt = null;
        UnpublishedAt = null;
        UpdatedAt = nowUtc;
        UpdatedByUserId = actorUserId;
        Version++;
    }

    public void Unpublish(string reason, DateTime nowUtc, long actorUserId)
    {
        EnsureNotDeleted();
        ValidateActorUserId(actorUserId);

        if (Status != ArticleStatuses.Published)
        {
            throw new ContentDomainException(
                "CONTENT.ARTICLE_NOT_PUBLISHED",
                "Only published articles can be unpublished.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ContentDomainException(
                "CONTENT.UNPUBLISH_REASON_REQUIRED",
                "Unpublish reason is required.");
        }

        Status = ArticleStatuses.Draft;
        UnpublishedAt = nowUtc;
        UpdatedAt = nowUtc;
        UpdatedByUserId = actorUserId;
        Version++;
    }

    public void Archive(DateTime nowUtc, long actorUserId)
    {
        EnsureNotDeleted();
        ValidateActorUserId(actorUserId);

        if (Status == ArticleStatuses.Archived)
        {
            throw new ContentDomainException(
                "CONTENT.ARTICLE_ALREADY_ARCHIVED",
                "Article is already archived.");
        }

        if (Status != ArticleStatuses.Draft &&
            Status != ArticleStatuses.Published)
        {
            throw new ContentDomainException(
                "CONTENT.INVALID_STATE_TRANSITION",
                "Only draft or published articles can be archived.");
        }

        Status = ArticleStatuses.Archived;
        ArchivedAt = nowUtc;
        UpdatedAt = nowUtc;
        UpdatedByUserId = actorUserId;
        Version++;
    }

    public void SoftDelete(DateTime nowUtc, long actorUserId)
    {
        ValidateActorUserId(actorUserId);

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

    private static void ValidateArticlePublicId(string articlePublicId)
    {
        if (string.IsNullOrWhiteSpace(articlePublicId))
        {
            throw new ContentDomainException(
                "CONTENT.ARTICLE_PUBLIC_ID_REQUIRED",
                "Article public id is required.");
        }

        if (articlePublicId.Trim().Length != 26)
        {
            throw new ContentDomainException(
                "CONTENT.ARTICLE_PUBLIC_ID_INVALID",
                "Article public id must be a 26-character ULID.");
        }
    }

    private static void ValidateCategoryId(long categoryId)
    {
        if (categoryId <= 0)
        {
            throw new ContentDomainException(
                "CONTENT.ARTICLE_CATEGORY_ID_INVALID",
                "Category id must be greater than zero.");
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

    private static void ValidateActorUserId(long actorUserId)
    {
        if (actorUserId <= 0)
        {
            throw new ContentDomainException(
                "CONTENT.ARTICLE_ACTOR_USER_ID_INVALID",
                "Actor user id must be greater than zero.");
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

    private static void ValidateSummary(string summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
        {
            throw new ContentDomainException(
                "CONTENT.ARTICLE_SUMMARY_REQUIRED",
                "Article summary is required.");
        }

        if (summary.Trim().Length > 1000)
        {
            throw new ContentDomainException(
                "CONTENT.ARTICLE_SUMMARY_TOO_LONG",
                "Article summary must not exceed 1000 characters.");
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
}