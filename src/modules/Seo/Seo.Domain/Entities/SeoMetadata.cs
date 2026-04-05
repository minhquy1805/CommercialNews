using Seo.Domain.Exceptions;

namespace Seo.Domain.Entities;

public sealed class SeoMetadata
{
    public long SeoId { get; private set; }

    public long ArticleId { get; private set; }

    public string? CanonicalUrl { get; private set; }
    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }

    public string? OgTitle { get; private set; }
    public string? OgDescription { get; private set; }
    public string? OgImageUrl { get; private set; }

    public string? TwitterTitle { get; private set; }
    public string? TwitterDescription { get; private set; }
    public string? TwitterImageUrl { get; private set; }

    public int Version { get; private set; }

    public DateTime UpdatedAt { get; private set; }
    public long? UpdatedByUserId { get; private set; }

    private SeoMetadata()
    {
    }

    public static SeoMetadata Create(
        long articleId,
        string? canonicalUrl,
        string? metaTitle,
        string? metaDescription,
        string? ogTitle,
        string? ogDescription,
        string? ogImageUrl,
        string? twitterTitle,
        string? twitterDescription,
        string? twitterImageUrl,
        DateTime nowUtc,
        long? actorUserId)
    {
        ValidateArticleId(articleId);
        ValidateCanonicalUrl(canonicalUrl);
        ValidateMetaTitle(metaTitle);
        ValidateMetaDescription(metaDescription);
        ValidateOgTitle(ogTitle);
        ValidateOgDescription(ogDescription);
        ValidateOgImageUrl(ogImageUrl);
        ValidateTwitterTitle(twitterTitle);
        ValidateTwitterDescription(twitterDescription);
        ValidateTwitterImageUrl(twitterImageUrl);

        return new SeoMetadata
        {
            ArticleId = articleId,
            CanonicalUrl = NormalizeOptional(canonicalUrl),
            MetaTitle = NormalizeOptional(metaTitle),
            MetaDescription = NormalizeOptional(metaDescription),
            OgTitle = NormalizeOptional(ogTitle),
            OgDescription = NormalizeOptional(ogDescription),
            OgImageUrl = NormalizeOptional(ogImageUrl),
            TwitterTitle = NormalizeOptional(twitterTitle),
            TwitterDescription = NormalizeOptional(twitterDescription),
            TwitterImageUrl = NormalizeOptional(twitterImageUrl),
            UpdatedAt = nowUtc,
            UpdatedByUserId = actorUserId,
            Version = 1
        };
    }

    public static SeoMetadata Rehydrate(
        long seoId,
        long articleId,
        string? canonicalUrl,
        string? metaTitle,
        string? metaDescription,
        string? ogTitle,
        string? ogDescription,
        string? ogImageUrl,
        string? twitterTitle,
        string? twitterDescription,
        string? twitterImageUrl,
        int version,
        DateTime updatedAt,
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
                "Seo metadata version must be greater than zero.");
        }

        ValidateArticleId(articleId);
        ValidateCanonicalUrl(canonicalUrl);
        ValidateMetaTitle(metaTitle);
        ValidateMetaDescription(metaDescription);
        ValidateOgTitle(ogTitle);
        ValidateOgDescription(ogDescription);
        ValidateOgImageUrl(ogImageUrl);
        ValidateTwitterTitle(twitterTitle);
        ValidateTwitterDescription(twitterDescription);
        ValidateTwitterImageUrl(twitterImageUrl);

        return new SeoMetadata
        {
            SeoId = seoId,
            ArticleId = articleId,
            CanonicalUrl = NormalizeOptional(canonicalUrl),
            MetaTitle = NormalizeOptional(metaTitle),
            MetaDescription = NormalizeOptional(metaDescription),
            OgTitle = NormalizeOptional(ogTitle),
            OgDescription = NormalizeOptional(ogDescription),
            OgImageUrl = NormalizeOptional(ogImageUrl),
            TwitterTitle = NormalizeOptional(twitterTitle),
            TwitterDescription = NormalizeOptional(twitterDescription),
            TwitterImageUrl = NormalizeOptional(twitterImageUrl),
            Version = version,
            UpdatedAt = updatedAt,
            UpdatedByUserId = updatedByUserId
        };
    }

    public void Update(
        string? canonicalUrl,
        string? metaTitle,
        string? metaDescription,
        string? ogTitle,
        string? ogDescription,
        string? ogImageUrl,
        string? twitterTitle,
        string? twitterDescription,
        string? twitterImageUrl,
        DateTime nowUtc,
        long? actorUserId)
    {
        ValidateCanonicalUrl(canonicalUrl);
        ValidateMetaTitle(metaTitle);
        ValidateMetaDescription(metaDescription);
        ValidateOgTitle(ogTitle);
        ValidateOgDescription(ogDescription);
        ValidateOgImageUrl(ogImageUrl);
        ValidateTwitterTitle(twitterTitle);
        ValidateTwitterDescription(twitterDescription);
        ValidateTwitterImageUrl(twitterImageUrl);

        CanonicalUrl = NormalizeOptional(canonicalUrl);
        MetaTitle = NormalizeOptional(metaTitle);
        MetaDescription = NormalizeOptional(metaDescription);
        OgTitle = NormalizeOptional(ogTitle);
        OgDescription = NormalizeOptional(ogDescription);
        OgImageUrl = NormalizeOptional(ogImageUrl);
        TwitterTitle = NormalizeOptional(twitterTitle);
        TwitterDescription = NormalizeOptional(twitterDescription);
        TwitterImageUrl = NormalizeOptional(twitterImageUrl);
        UpdatedAt = nowUtc;
        UpdatedByUserId = actorUserId;
        Version++;
    }

    private static void ValidateArticleId(long articleId)
    {
        if (articleId <= 0)
        {
            throw new SeoDomainException(
                "SEO.SEO_METADATA_INVALID_ARTICLE_ID",
                "Article id must be greater than zero.");
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

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}