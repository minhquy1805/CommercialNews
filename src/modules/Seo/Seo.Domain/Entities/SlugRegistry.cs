using Seo.Domain.Enums;
using Seo.Domain.Exceptions;

namespace Seo.Domain.Entities;

public sealed class SlugRegistry
{
    public long SlugId { get; private set; }

    public long ArticleId { get; private set; }

    public string Slug { get; private set; } = null!;
    public string Scope { get; private set; } = null!;
    public string? CanonicalUrl { get; private set; }

    public bool IsIndexable { get; private set; }
    public bool IsActive { get; private set; }

    public int Version { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public long? CreatedByUserId { get; private set; }

    public DateTime UpdatedAt { get; private set; }
    public long? UpdatedByUserId { get; private set; }

    private SlugRegistry()
    {
    }

    public static SlugRegistry Create(
        long articleId,
        string slug,
        string scope,
        string? canonicalUrl,
        bool isIndexable,
        bool isActive,
        DateTime nowUtc,
        long? actorUserId)
    {
        ValidateArticleId(articleId);
        ValidateSlug(slug);
        ValidateScope(scope);
        ValidateCanonicalUrl(canonicalUrl);

        return new SlugRegistry
        {
            ArticleId = articleId,
            Slug = NormalizeRequired(slug),
            Scope = NormalizeRequired(scope),
            CanonicalUrl = NormalizeOptional(canonicalUrl),
            IsIndexable = isIndexable,
            IsActive = isActive,
            CreatedAt = nowUtc,
            CreatedByUserId = actorUserId,
            UpdatedAt = nowUtc,
            UpdatedByUserId = actorUserId,
            Version = 1
        };
    }

    public static SlugRegistry Rehydrate(
        long slugId,
        long articleId,
        string slug,
        string scope,
        string? canonicalUrl,
        bool isIndexable,
        bool isActive,
        int version,
        DateTime createdAt,
        long? createdByUserId,
        DateTime updatedAt,
        long? updatedByUserId)
    {
        if (slugId <= 0)
        {
            throw new SeoDomainException(
                "SEO.SLUG_REGISTRY_INVALID_SLUG_ID",
                "Slug id must be greater than zero.");
        }

        if (version <= 0)
        {
            throw new SeoDomainException(
                "SEO.SLUG_REGISTRY_INVALID_VERSION",
                "Slug registry version must be greater than zero.");
        }

        ValidateArticleId(articleId);
        ValidateSlug(slug);
        ValidateScope(scope);
        ValidateCanonicalUrl(canonicalUrl);

        if (updatedAt < createdAt)
        {
            throw new SeoDomainException(
                "SEO.SLUG_REGISTRY_INVALID_UPDATED_AT",
                "UpdatedAt must be greater than or equal to CreatedAt.");
        }

        return new SlugRegistry
        {
            SlugId = slugId,
            ArticleId = articleId,
            Slug = NormalizeRequired(slug),
            Scope = NormalizeRequired(scope),
            CanonicalUrl = NormalizeOptional(canonicalUrl),
            IsIndexable = isIndexable,
            IsActive = isActive,
            Version = version,
            CreatedAt = createdAt,
            CreatedByUserId = createdByUserId,
            UpdatedAt = updatedAt,
            UpdatedByUserId = updatedByUserId
        };
    }

    public void UpdateRouting(
        string slug,
        string scope,
        string? canonicalUrl,
        bool isIndexable,
        bool isActive,
        DateTime nowUtc,
        long? actorUserId)
    {
        ValidateSlug(slug);
        ValidateScope(scope);
        ValidateCanonicalUrl(canonicalUrl);

        Slug = NormalizeRequired(slug);
        Scope = NormalizeRequired(scope);
        CanonicalUrl = NormalizeOptional(canonicalUrl);
        IsIndexable = isIndexable;
        IsActive = isActive;
        UpdatedAt = nowUtc;
        UpdatedByUserId = actorUserId;
        Version++;
    }

    public void ChangeSlug(
        string slug,
        string? canonicalUrl,
        DateTime nowUtc,
        long? actorUserId)
    {
        EnsureActive();

        ValidateSlug(slug);
        ValidateCanonicalUrl(canonicalUrl);

        Slug = NormalizeRequired(slug);
        CanonicalUrl = NormalizeOptional(canonicalUrl);
        UpdatedAt = nowUtc;
        UpdatedByUserId = actorUserId;
        Version++;
    }

    public void ChangeCanonicalUrl(
        string? canonicalUrl,
        DateTime nowUtc,
        long? actorUserId)
    {
        ValidateCanonicalUrl(canonicalUrl);

        CanonicalUrl = NormalizeOptional(canonicalUrl);
        UpdatedAt = nowUtc;
        UpdatedByUserId = actorUserId;
        Version++;
    }

    public void MarkIndexable(DateTime nowUtc, long? actorUserId)
    {
        if (IsIndexable)
        {
            return;
        }

        IsIndexable = true;
        UpdatedAt = nowUtc;
        UpdatedByUserId = actorUserId;
        Version++;
    }

    public void MarkNonIndexable(DateTime nowUtc, long? actorUserId)
    {
        if (!IsIndexable)
        {
            return;
        }

        IsIndexable = false;
        UpdatedAt = nowUtc;
        UpdatedByUserId = actorUserId;
        Version++;
    }

    public void Activate(DateTime nowUtc, long? actorUserId)
    {
        if (IsActive)
        {
            throw new SeoDomainException(
                "SEO.SLUG_REGISTRY_ALREADY_ACTIVE",
                "Slug route is already active.");
        }

        IsActive = true;
        UpdatedAt = nowUtc;
        UpdatedByUserId = actorUserId;
        Version++;
    }

    public void Deactivate(DateTime nowUtc, long? actorUserId)
    {
        if (!IsActive)
        {
            throw new SeoDomainException(
                "SEO.SLUG_REGISTRY_ALREADY_INACTIVE",
                "Slug route is already inactive.");
        }

        IsActive = false;
        UpdatedAt = nowUtc;
        UpdatedByUserId = actorUserId;
        Version++;
    }

    private void EnsureActive()
    {
        if (!IsActive)
        {
            throw new SeoDomainException(
                "SEO.SLUG_REGISTRY_INACTIVE",
                "Slug route is inactive.");
        }
    }

    private static void ValidateArticleId(long articleId)
    {
        if (articleId <= 0)
        {
            throw new SeoDomainException(
                "SEO.SLUG_REGISTRY_INVALID_ARTICLE_ID",
                "Article id must be greater than zero.");
        }
    }

    private static void ValidateSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new SeoDomainException(
                "SEO.INVALID_SLUG",
                "Slug is required.");
        }

        if (slug.Trim().Length > 200)
        {
            throw new SeoDomainException(
                "SEO.SLUG_TOO_LONG",
                "Slug must not exceed 200 characters.");
        }
    }

    private static void ValidateScope(string scope)
    {
        if (!SeoScopes.IsValid(scope))
        {
            throw new SeoDomainException(
                "SEO.INVALID_SCOPE",
                "Scope is invalid.");
        }
    }

    private static void ValidateCanonicalUrl(string? canonicalUrl)
    {
        if (!string.IsNullOrWhiteSpace(canonicalUrl) && canonicalUrl.Trim().Length > 500)
        {
            throw new SeoDomainException(
                "SEO.CANONICAL_URL_TOO_LONG",
                "Canonical URL must not exceed 500 characters.");
        }
    }

    private static string NormalizeRequired(string value)
    {
        return value.Trim();
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