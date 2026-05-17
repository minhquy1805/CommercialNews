using Seo.Domain.Constants;
using Seo.Domain.Exceptions;

namespace Seo.Domain.Entities;

public sealed class SeoMetadata
{
    public long SeoId { get; private set; }

    public string Scope { get; private set; } = default!;
    public string ResourceType { get; private set; } = default!;
    public string ResourcePublicId { get; private set; } = default!;

    public string? Slug { get; private set; }
    public string? CanonicalUrl { get; private set; }

    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }

    public string? OgTitle { get; private set; }
    public string? OgDescription { get; private set; }
    public string? OgImageUrl { get; private set; }

    public string? TwitterTitle { get; private set; }
    public string? TwitterDescription { get; private set; }
    public string? TwitterImageUrl { get; private set; }

    public string? Robots { get; private set; }

    public bool IsManualOverride { get; private set; }

    public long? SourceAggregateVersion { get; private set; }
    public string? LastAppliedMessageId { get; private set; }
    public DateTime? LastSyncedAtUtc { get; private set; }

    public int Version { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public long? UpdatedByUserId { get; private set; }

    private SeoMetadata()
    {
    }

    public static SeoMetadata Create(
        string? scope,
        string resourceType,
        string resourcePublicId,
        string? slug,
        string? canonicalUrl,
        string? metaTitle,
        string? metaDescription,
        string? ogTitle,
        string? ogDescription,
        string? ogImageUrl,
        string? twitterTitle,
        string? twitterDescription,
        string? twitterImageUrl,
        string? robots,
        bool isManualOverride,
        DateTime nowUtc,
        long? actorUserId)
    {
        var normalizedScope = NormalizeScope(scope);

        ValidateResourceType(resourceType);
        ValidateResourcePublicId(resourcePublicId);
        ValidateSlug(slug);
        ValidateCanonicalUrl(canonicalUrl);
        ValidateMetaTitle(metaTitle);
        ValidateMetaDescription(metaDescription);
        ValidateOgTitle(ogTitle);
        ValidateOgDescription(ogDescription);
        ValidateOgImageUrl(ogImageUrl);
        ValidateTwitterTitle(twitterTitle);
        ValidateTwitterDescription(twitterDescription);
        ValidateTwitterImageUrl(twitterImageUrl);
        ValidateRobots(robots);

        return new SeoMetadata
        {
            Scope = normalizedScope,
            ResourceType = resourceType.Trim(),
            ResourcePublicId = resourcePublicId.Trim(),
            Slug = NormalizeOptional(slug),
            CanonicalUrl = NormalizeOptional(canonicalUrl),
            MetaTitle = NormalizeOptional(metaTitle),
            MetaDescription = NormalizeOptional(metaDescription),
            OgTitle = NormalizeOptional(ogTitle),
            OgDescription = NormalizeOptional(ogDescription),
            OgImageUrl = NormalizeOptional(ogImageUrl),
            TwitterTitle = NormalizeOptional(twitterTitle),
            TwitterDescription = NormalizeOptional(twitterDescription),
            TwitterImageUrl = NormalizeOptional(twitterImageUrl),
            Robots = NormalizeOptional(robots),
            IsManualOverride = isManualOverride,
            Version = 1,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
            UpdatedByUserId = actorUserId
        };
    }

    public static SeoMetadata Rehydrate(
        long seoId,
        string scope,
        string resourceType,
        string resourcePublicId,
        string? slug,
        string? canonicalUrl,
        string? metaTitle,
        string? metaDescription,
        string? ogTitle,
        string? ogDescription,
        string? ogImageUrl,
        string? twitterTitle,
        string? twitterDescription,
        string? twitterImageUrl,
        string? robots,
        bool isManualOverride,
        long? sourceAggregateVersion,
        string? lastAppliedMessageId,
        DateTime? lastSyncedAtUtc,
        int version,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        long? updatedByUserId)
    {
        if (seoId <= 0)
        {
            throw new SeoDomainException(
                "SEO.SEO_METADATA_INVALID_SEO_ID",
                "Seo id must be greater than zero.");
        }

        if (version <= 0)
        {
            throw new SeoDomainException(
                "SEO.SEO_METADATA_INVALID_VERSION",
                "SEO metadata version must be greater than zero.");
        }

        if (updatedAtUtc < createdAtUtc)
        {
            throw new SeoDomainException(
                "SEO.SEO_METADATA_INVALID_UPDATED_AT",
                "Updated time is invalid for the current SEO metadata state.");
        }

        ValidateScope(scope);
        ValidateResourceType(resourceType);
        ValidateResourcePublicId(resourcePublicId);
        ValidateSlug(slug);
        ValidateCanonicalUrl(canonicalUrl);
        ValidateMetaTitle(metaTitle);
        ValidateMetaDescription(metaDescription);
        ValidateOgTitle(ogTitle);
        ValidateOgDescription(ogDescription);
        ValidateOgImageUrl(ogImageUrl);
        ValidateTwitterTitle(twitterTitle);
        ValidateTwitterDescription(twitterDescription);
        ValidateTwitterImageUrl(twitterImageUrl);
        ValidateRobots(robots);
        ValidateSourceAggregateVersion(sourceAggregateVersion);
        ValidateLastAppliedMessageId(lastAppliedMessageId);

        return new SeoMetadata
        {
            SeoId = seoId,
            Scope = scope.Trim(),
            ResourceType = resourceType.Trim(),
            ResourcePublicId = resourcePublicId.Trim(),
            Slug = NormalizeOptional(slug),
            CanonicalUrl = NormalizeOptional(canonicalUrl),
            MetaTitle = NormalizeOptional(metaTitle),
            MetaDescription = NormalizeOptional(metaDescription),
            OgTitle = NormalizeOptional(ogTitle),
            OgDescription = NormalizeOptional(ogDescription),
            OgImageUrl = NormalizeOptional(ogImageUrl),
            TwitterTitle = NormalizeOptional(twitterTitle),
            TwitterDescription = NormalizeOptional(twitterDescription),
            TwitterImageUrl = NormalizeOptional(twitterImageUrl),
            Robots = NormalizeOptional(robots),
            IsManualOverride = isManualOverride,
            SourceAggregateVersion = sourceAggregateVersion,
            LastAppliedMessageId = NormalizeOptional(lastAppliedMessageId),
            LastSyncedAtUtc = lastSyncedAtUtc,
            Version = version,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = updatedAtUtc,
            UpdatedByUserId = updatedByUserId
        };
    }

    public void UpdateManualMetadata(
        string? slug,
        string? canonicalUrl,
        string? metaTitle,
        string? metaDescription,
        string? ogTitle,
        string? ogDescription,
        string? ogImageUrl,
        string? twitterTitle,
        string? twitterDescription,
        string? twitterImageUrl,
        string? robots,
        DateTime nowUtc,
        long? actorUserId)
    {
        ValidateSlug(slug);
        ValidateCanonicalUrl(canonicalUrl);
        ValidateMetaTitle(metaTitle);
        ValidateMetaDescription(metaDescription);
        ValidateOgTitle(ogTitle);
        ValidateOgDescription(ogDescription);
        ValidateOgImageUrl(ogImageUrl);
        ValidateTwitterTitle(twitterTitle);
        ValidateTwitterDescription(twitterDescription);
        ValidateTwitterImageUrl(twitterImageUrl);
        ValidateRobots(robots);

        Slug = NormalizeOptional(slug);
        CanonicalUrl = NormalizeOptional(canonicalUrl);
        MetaTitle = NormalizeOptional(metaTitle);
        MetaDescription = NormalizeOptional(metaDescription);
        OgTitle = NormalizeOptional(ogTitle);
        OgDescription = NormalizeOptional(ogDescription);
        OgImageUrl = NormalizeOptional(ogImageUrl);
        TwitterTitle = NormalizeOptional(twitterTitle);
        TwitterDescription = NormalizeOptional(twitterDescription);
        TwitterImageUrl = NormalizeOptional(twitterImageUrl);
        Robots = NormalizeOptional(robots);
        IsManualOverride = true;
        UpdatedAtUtc = nowUtc;
        UpdatedByUserId = actorUserId;
        Version++;
    }

    public void MarkAutoSynced(
        long sourceAggregateVersion,
        string lastAppliedMessageId,
        DateTime lastSyncedAtUtc,
        DateTime nowUtc)
    {
        ValidateSourceAggregateVersion(sourceAggregateVersion);
        ValidateLastAppliedMessageId(lastAppliedMessageId);

        SourceAggregateVersion = sourceAggregateVersion;
        LastAppliedMessageId = lastAppliedMessageId.Trim();
        LastSyncedAtUtc = lastSyncedAtUtc;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    private static string NormalizeScope(string? scope)
    {
        if (string.IsNullOrWhiteSpace(scope))
        {
            return SeoScopes.Public;
        }

        var normalized = scope.Trim();

        ValidateScope(normalized);

        return normalized;
    }

    private static void ValidateScope(string? scope)
    {
        if (!SeoScopes.IsValid(scope))
        {
            throw new SeoDomainException(
                "SEO.INVALID_SCOPE",
                "Scope is invalid.");
        }
    }

    private static void ValidateResourceType(string? resourceType)
    {
        if (!SeoResourceTypes.IsValid(resourceType))
        {
            throw new SeoDomainException(
                "SEO.INVALID_RESOURCE_TYPE",
                "Resource type is invalid.");
        }
    }

    private static void ValidateResourcePublicId(string? resourcePublicId)
    {
        if (string.IsNullOrWhiteSpace(resourcePublicId))
        {
            throw new SeoDomainException(
                "SEO.INVALID_RESOURCE_PUBLIC_ID",
                "Resource public id is required.");
        }

        if (resourcePublicId.Trim().Length != 26)
        {
            throw new SeoDomainException(
                "SEO.INVALID_RESOURCE_PUBLIC_ID",
                "Resource public id must be 26 characters.");
        }
    }

    private static void ValidateSlug(string? slug)
    {
        if (!string.IsNullOrWhiteSpace(slug) && slug.Trim().Length > 200)
        {
            throw new SeoDomainException(
                "SEO.SLUG_TOO_LONG",
                "Slug must not exceed 200 characters.");
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

    private static void ValidateMetaTitle(string? metaTitle)
    {
        if (!string.IsNullOrWhiteSpace(metaTitle) && metaTitle.Trim().Length > 300)
        {
            throw new SeoDomainException(
                "SEO.META_TITLE_TOO_LONG",
                "Meta title must not exceed 300 characters.");
        }
    }

    private static void ValidateMetaDescription(string? metaDescription)
    {
        if (!string.IsNullOrWhiteSpace(metaDescription) && metaDescription.Trim().Length > 500)
        {
            throw new SeoDomainException(
                "SEO.META_DESCRIPTION_TOO_LONG",
                "Meta description must not exceed 500 characters.");
        }
    }

    private static void ValidateOgTitle(string? ogTitle)
    {
        if (!string.IsNullOrWhiteSpace(ogTitle) && ogTitle.Trim().Length > 300)
        {
            throw new SeoDomainException(
                "SEO.OG_TITLE_TOO_LONG",
                "OG title must not exceed 300 characters.");
        }
    }

    private static void ValidateOgDescription(string? ogDescription)
    {
        if (!string.IsNullOrWhiteSpace(ogDescription) && ogDescription.Trim().Length > 500)
        {
            throw new SeoDomainException(
                "SEO.OG_DESCRIPTION_TOO_LONG",
                "OG description must not exceed 500 characters.");
        }
    }

    private static void ValidateOgImageUrl(string? ogImageUrl)
    {
        if (!string.IsNullOrWhiteSpace(ogImageUrl) && ogImageUrl.Trim().Length > 800)
        {
            throw new SeoDomainException(
                "SEO.OG_IMAGE_URL_TOO_LONG",
                "OG image URL must not exceed 800 characters.");
        }
    }

    private static void ValidateTwitterTitle(string? twitterTitle)
    {
        if (!string.IsNullOrWhiteSpace(twitterTitle) && twitterTitle.Trim().Length > 300)
        {
            throw new SeoDomainException(
                "SEO.TWITTER_TITLE_TOO_LONG",
                "Twitter title must not exceed 300 characters.");
        }
    }

    private static void ValidateTwitterDescription(string? twitterDescription)
    {
        if (!string.IsNullOrWhiteSpace(twitterDescription) && twitterDescription.Trim().Length > 500)
        {
            throw new SeoDomainException(
                "SEO.TWITTER_DESCRIPTION_TOO_LONG",
                "Twitter description must not exceed 500 characters.");
        }
    }

    private static void ValidateTwitterImageUrl(string? twitterImageUrl)
    {
        if (!string.IsNullOrWhiteSpace(twitterImageUrl) && twitterImageUrl.Trim().Length > 800)
        {
            throw new SeoDomainException(
                "SEO.TWITTER_IMAGE_URL_TOO_LONG",
                "Twitter image URL must not exceed 800 characters.");
        }
    }

    private static void ValidateRobots(string? robots)
    {
        if (!string.IsNullOrWhiteSpace(robots) && robots.Trim().Length > 100)
        {
            throw new SeoDomainException(
                "SEO.ROBOTS_TOO_LONG",
                "Robots directive must not exceed 100 characters.");
        }
    }

    private static void ValidateSourceAggregateVersion(long? sourceAggregateVersion)
    {
        if (sourceAggregateVersion is not null && sourceAggregateVersion <= 0)
        {
            throw new SeoDomainException(
                "SEO.INVALID_SOURCE_AGGREGATE_VERSION",
                "Source aggregate version must be greater than zero.");
        }
    }

    private static void ValidateLastAppliedMessageId(string? lastAppliedMessageId)
    {
        if (!string.IsNullOrWhiteSpace(lastAppliedMessageId) && lastAppliedMessageId.Trim().Length != 26)
        {
            throw new SeoDomainException(
                "SEO.INVALID_LAST_APPLIED_MESSAGE_ID",
                "Last applied message id must be 26 characters.");
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