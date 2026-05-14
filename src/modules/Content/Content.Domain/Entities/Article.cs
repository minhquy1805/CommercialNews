using Content.Domain.Common;
using Content.Domain.Constants;
using Content.Domain.Exceptions;

namespace Content.Domain.Entities;

public sealed class Article : ContentSoftDeletableEntity
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

    private Article()
    {
    }

    public bool IsDraft => Status == ArticleStatuses.Draft;

    public bool IsPublished => Status == ArticleStatuses.Published;

    public bool IsArchived => Status == ArticleStatuses.Archived;

    public bool IsPubliclyVisible => IsPublished && !IsDeleted;

    public bool CanUpdate()
    {
        return !IsDeleted && IsDraft;
    }

    public bool CanPublish()
    {
        return !IsDeleted && IsDraft;
    }

    public bool CanUnpublish()
    {
        return !IsDeleted && IsPublished;
    }

    public bool CanArchive()
    {
        return !IsDeleted && (IsDraft || IsPublished);
    }

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
        string normalizedPublicId = ValidateArticlePublicId(articlePublicId);
        ValidateCategoryId(categoryId);
        ValidateAuthorUserId(authorUserId);
        ValidateActorUserId(actorUserId);
        ValidateTitle(title);
        ValidateSummary(summary);
        ValidateBody(body);
        ValidateCoverMediaId(coverMediaId);

        var article = new Article
        {
            ArticlePublicId = normalizedPublicId,
            CategoryId = categoryId,
            AuthorUserId = authorUserId,
            Title = ContentText.NormalizeRequired(title),
            Summary = ContentText.NormalizeRequired(summary),
            Body = ContentText.NormalizeRequired(body),
            Status = ArticleStatuses.Draft,
            PublishedAt = null,
            UnpublishedAt = null,
            ArchivedAt = null,
            CoverMediaId = coverMediaId
        };

        article.InitializeTracking(nowUtc, actorUserId);
        article.InitializeDeletion();

        return article;
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
        ContentGuard.AgainstInvalidId(
            articleId,
            "CONTENT.ARTICLE_INVALID_ARTICLE_ID",
            "Article id must be greater than zero.");

        string normalizedPublicId = ValidateArticlePublicId(articlePublicId);
        string normalizedStatus = ArticleStatuses.Normalize(status);
        ValidateCategoryId(categoryId);
        ValidateAuthorUserId(authorUserId);
        ValidateActorUserId(createdByUserId);
        ValidateTitle(title);
        ValidateSummary(summary);
        ValidateBody(body);
        ValidateCoverMediaId(coverMediaId);

        ContentGuard.AgainstInvalidVersion(
            version,
            "CONTENT.ARTICLE_INVALID_VERSION",
            "Article version must be greater than zero.");

        ContentGuard.AgainstUpdatedBeforeCreated(
            updatedAt,
            createdAt,
            "CONTENT.ARTICLE_INVALID_UPDATED_AT",
            "UpdatedAt cannot be earlier than CreatedAt.");

        ContentGuard.AgainstDeletedBeforeCreated(
            deletedAt,
            createdAt,
            "CONTENT.ARTICLE_INVALID_DELETED_AT",
            "DeletedAt cannot be earlier than CreatedAt.");

        var article = new Article
        {
            ArticleId = articleId,
            ArticlePublicId = normalizedPublicId,
            CategoryId = categoryId,
            AuthorUserId = authorUserId,
            Title = ContentText.NormalizeRequired(title),
            Summary = ContentText.NormalizeRequired(summary),
            Body = ContentText.NormalizeRequired(body),
            Status = normalizedStatus,
            PublishedAt = publishedAt,
            UnpublishedAt = unpublishedAt,
            ArchivedAt = archivedAt,
            CoverMediaId = coverMediaId
        };

        article.RehydrateTracking(
            createdAt,
            updatedAt,
            createdByUserId,
            updatedByUserId,
            version);
        article.RehydrateDeletion(isDeleted, deletedAt, deletedByUserId);

        return article;
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
        EnsureArticleNotDeleted();

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
        ValidateCoverMediaId(coverMediaId);
        ValidateActorUserId(actorUserId);

        CategoryId = categoryId;
        Title = ContentText.NormalizeRequired(title);
        Summary = ContentText.NormalizeRequired(summary);
        Body = ContentText.NormalizeRequired(body);
        CoverMediaId = coverMediaId;
        MarkUpdated(nowUtc, actorUserId);
    }

    public void Publish(DateTime nowUtc, long actorUserId)
    {
        EnsureArticleNotDeleted();
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
        MarkUpdated(nowUtc, actorUserId);
    }

    public void Unpublish(string reason, DateTime nowUtc, long actorUserId)
    {
        EnsureArticleNotDeleted();
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
        MarkUpdated(nowUtc, actorUserId);
    }

    public void Archive(DateTime nowUtc, long actorUserId)
    {
        EnsureArticleNotDeleted();
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
        MarkUpdated(nowUtc, actorUserId);
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

        MarkDeleted(nowUtc, actorUserId);
    }

    private void EnsureArticleNotDeleted()
    {
        EnsureNotDeleted(
            "CONTENT.ARTICLE_ALREADY_DELETED",
            "Article is already deleted.");
    }

    private static string ValidateArticlePublicId(string articlePublicId)
    {
        return PublicIdRules.ValidateAndNormalize(
            articlePublicId,
            "CONTENT.ARTICLE_PUBLIC_ID_REQUIRED",
            "CONTENT.ARTICLE_PUBLIC_ID_INVALID",
            "Article public id");
    }

    private static void ValidateCategoryId(long categoryId)
    {
        ContentGuard.AgainstInvalidId(
            categoryId,
            "CONTENT.ARTICLE_CATEGORY_ID_INVALID",
            "Category id must be greater than zero.");
    }

    private static void ValidateAuthorUserId(long authorUserId)
    {
        ContentGuard.AgainstInvalidId(
            authorUserId,
            "CONTENT.ARTICLE_AUTHOR_USER_ID_INVALID",
            "Author user id must be greater than zero.");
    }

    private static void ValidateActorUserId(long actorUserId)
    {
        ContentGuard.AgainstInvalidId(
            actorUserId,
            "CONTENT.ARTICLE_ACTOR_USER_ID_INVALID",
            "Actor user id must be greater than zero.");
    }

    private static void ValidateTitle(string title)
    {
        ContentGuard.AgainstRequiredText(
            title,
            "CONTENT.ARTICLE_TITLE_REQUIRED",
            "Article title is required.");
        ContentGuard.AgainstTooLong(
            title,
            ContentFieldLimits.ArticleTitleMaxLength,
            "CONTENT.ARTICLE_TITLE_TOO_LONG",
            $"Article title must not exceed {ContentFieldLimits.ArticleTitleMaxLength} characters.");
    }

    private static void ValidateSummary(string summary)
    {
        ContentGuard.AgainstRequiredText(
            summary,
            "CONTENT.ARTICLE_SUMMARY_REQUIRED",
            "Article summary is required.");
        ContentGuard.AgainstTooLong(
            summary,
            ContentFieldLimits.ArticleSummaryMaxLength,
            "CONTENT.ARTICLE_SUMMARY_TOO_LONG",
            $"Article summary must not exceed {ContentFieldLimits.ArticleSummaryMaxLength} characters.");
    }

    private static void ValidateBody(string body)
    {
        ContentGuard.AgainstRequiredText(
            body,
            "CONTENT.ARTICLE_BODY_REQUIRED",
            "Article body is required.");
    }

    private static void ValidateCoverMediaId(long? coverMediaId)
    {
        ContentGuard.AgainstInvalidOptionalId(
            coverMediaId,
            "CONTENT.ARTICLE_COVER_MEDIA_ID_INVALID",
            "Cover media id must be greater than zero.");
    }
}
