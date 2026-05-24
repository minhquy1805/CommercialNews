using Reading.Domain.Constants;
using Reading.Domain.Exceptions;

namespace Reading.Domain.Entities;

public sealed class ArticleSeoMetadataProjection
{
    private const int PublicIdLength = 26;

    private const int MaxMetaTitleLength = 300;
    private const int MaxMetaDescriptionLength = 500;

    private const int MaxOgTitleLength = 300;
    private const int MaxOgDescriptionLength = 500;
    private const int MaxOgImageUrlLength = 800;

    private const int MaxTwitterTitleLength = 300;
    private const int MaxTwitterDescriptionLength = 500;
    private const int MaxTwitterImageUrlLength = 800;

    private const int MaxRobotsLength = 100;

    private ArticleSeoMetadataProjection()
    {
    }

    public string Scope { get; private set; } = string.Empty;

    public string ResourceType { get; private set; } = string.Empty;

    public string ResourcePublicId { get; private set; } = string.Empty;

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

    /// <summary>
    /// Version of the SEO metadata aggregate only.
    /// This is not the Content article version and not the SEO route version.
    /// </summary>
    public long SourceVersion { get; private set; }

    /// <summary>
    /// Message id of the latest applied SEO metadata event.
    /// </summary>
    public string? LastEventMessageId { get; private set; }

    public DateTime? LastSourceOccurredAtUtc { get; private set; }

    public DateTime LastSyncedAtUtc { get; private set; }

    /// <summary>
    /// Creates or rehydrates an SEO metadata projection row.
    /// Stored source version may be zero for initialized or reconstructed state.
    /// </summary>
    public static ArticleSeoMetadataProjection Create(
        string scope,
        string resourceType,
        string resourcePublicId,
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
        long sourceVersion,
        string? lastEventMessageId,
        DateTime? lastSourceOccurredAtUtc,
        DateTime lastSyncedAtUtc)
    {
        string normalizedScope = ValidateAndNormalizeScope(scope);
        string normalizedResourceType = ValidateAndNormalizeResourceType(resourceType);

        ValidateResourcePublicId(resourcePublicId);
        ValidateMetadata(
            metaTitle,
            metaDescription,
            ogTitle,
            ogDescription,
            ogImageUrl,
            twitterTitle,
            twitterDescription,
            twitterImageUrl,
            robots);

        ValidateStoredSourceVersion(sourceVersion);
        ValidateMessageId(lastEventMessageId);
        ValidateLastSyncedAtUtc(lastSyncedAtUtc);

        return new ArticleSeoMetadataProjection
        {
            Scope = normalizedScope,
            ResourceType = normalizedResourceType,
            ResourcePublicId = resourcePublicId.Trim(),

            MetaTitle = NormalizeNullable(metaTitle),
            MetaDescription = NormalizeNullable(metaDescription),

            OgTitle = NormalizeNullable(ogTitle),
            OgDescription = NormalizeNullable(ogDescription),
            OgImageUrl = NormalizeNullable(ogImageUrl),

            TwitterTitle = NormalizeNullable(twitterTitle),
            TwitterDescription = NormalizeNullable(twitterDescription),
            TwitterImageUrl = NormalizeNullable(twitterImageUrl),

            Robots = NormalizeNullable(robots),
            IsManualOverride = isManualOverride,

            SourceVersion = sourceVersion,
            LastEventMessageId = NormalizeNullable(lastEventMessageId),
            LastSourceOccurredAtUtc = lastSourceOccurredAtUtc,
            LastSyncedAtUtc = lastSyncedAtUtc
        };
    }

    public bool CanApply(long incomingSourceVersion)
    {
        ValidateIncomingSourceVersion(incomingSourceVersion);

        return incomingSourceVersion > SourceVersion;
    }

    public bool Apply(
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
        long sourceVersion,
        string? messageId,
        DateTime? sourceOccurredAtUtc,
        DateTime lastSyncedAtUtc)
    {
        ValidateMetadata(
            metaTitle,
            metaDescription,
            ogTitle,
            ogDescription,
            ogImageUrl,
            twitterTitle,
            twitterDescription,
            twitterImageUrl,
            robots);

        ValidateIncomingSourceVersion(sourceVersion);
        ValidateMessageId(messageId);
        ValidateLastSyncedAtUtc(lastSyncedAtUtc);

        if (!CanApply(sourceVersion))
        {
            return false;
        }

        MetaTitle = NormalizeNullable(metaTitle);
        MetaDescription = NormalizeNullable(metaDescription);

        OgTitle = NormalizeNullable(ogTitle);
        OgDescription = NormalizeNullable(ogDescription);
        OgImageUrl = NormalizeNullable(ogImageUrl);

        TwitterTitle = NormalizeNullable(twitterTitle);
        TwitterDescription = NormalizeNullable(twitterDescription);
        TwitterImageUrl = NormalizeNullable(twitterImageUrl);

        Robots = NormalizeNullable(robots);
        IsManualOverride = isManualOverride;

        SourceVersion = sourceVersion;
        LastEventMessageId = NormalizeNullable(messageId);
        LastSourceOccurredAtUtc = sourceOccurredAtUtc;
        LastSyncedAtUtc = lastSyncedAtUtc;

        return true;
    }

    private static string ValidateAndNormalizeScope(string scope)
    {
        string? normalized = ReadingProjectionScopes.NormalizeOrNull(scope);

        if (normalized is null)
        {
            throw new ReadingDomainException(
                "READING.INVALID_SEO_METADATA_SCOPE",
                "Only public SEO scope is supported by Reading article metadata projection.");
        }

        return normalized;
    }

    private static string ValidateAndNormalizeResourceType(string resourceType)
    {
        string? normalized = ReadingProjectionResourceTypes.NormalizeOrNull(resourceType);

        if (normalized is null)
        {
            throw new ReadingDomainException(
                "READING.INVALID_SEO_METADATA_RESOURCE_TYPE",
                "Only Article SEO resource type is supported by Reading article metadata projection.");
        }

        return normalized;
    }

    private static void ValidateResourcePublicId(string resourcePublicId)
    {
        if (string.IsNullOrWhiteSpace(resourcePublicId)
            || resourcePublicId.Trim().Length != PublicIdLength)
        {
            throw new ReadingDomainException(
                "READING.INVALID_SEO_METADATA_RESOURCE_PUBLIC_ID",
                "SEO metadata resource public id must be a 26-character value.");
        }
    }

    private static void ValidateMetadata(
        string? metaTitle,
        string? metaDescription,
        string? ogTitle,
        string? ogDescription,
        string? ogImageUrl,
        string? twitterTitle,
        string? twitterDescription,
        string? twitterImageUrl,
        string? robots)
    {
        ValidateOptionalLength(
            metaTitle,
            MaxMetaTitleLength,
            "READING.SEO_META_TITLE_TOO_LONG",
            "SEO meta title");

        ValidateOptionalLength(
            metaDescription,
            MaxMetaDescriptionLength,
            "READING.SEO_META_DESCRIPTION_TOO_LONG",
            "SEO meta description");

        ValidateOptionalLength(
            ogTitle,
            MaxOgTitleLength,
            "READING.SEO_OG_TITLE_TOO_LONG",
            "SEO Open Graph title");

        ValidateOptionalLength(
            ogDescription,
            MaxOgDescriptionLength,
            "READING.SEO_OG_DESCRIPTION_TOO_LONG",
            "SEO Open Graph description");

        ValidateOptionalLength(
            ogImageUrl,
            MaxOgImageUrlLength,
            "READING.SEO_OG_IMAGE_URL_TOO_LONG",
            "SEO Open Graph image URL");

        ValidateOptionalLength(
            twitterTitle,
            MaxTwitterTitleLength,
            "READING.SEO_TWITTER_TITLE_TOO_LONG",
            "SEO Twitter title");

        ValidateOptionalLength(
            twitterDescription,
            MaxTwitterDescriptionLength,
            "READING.SEO_TWITTER_DESCRIPTION_TOO_LONG",
            "SEO Twitter description");

        ValidateOptionalLength(
            twitterImageUrl,
            MaxTwitterImageUrlLength,
            "READING.SEO_TWITTER_IMAGE_URL_TOO_LONG",
            "SEO Twitter image URL");

        ValidateOptionalLength(
            robots,
            MaxRobotsLength,
            "READING.SEO_ROBOTS_TOO_LONG",
            "SEO robots value");
    }

    private static void ValidateOptionalLength(
        string? value,
        int maxLength,
        string errorCode,
        string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (value.Trim().Length > maxLength)
        {
            throw new ReadingDomainException(
                errorCode,
                $"{fieldName} must not exceed {maxLength} characters.");
        }
    }

    private static void ValidateStoredSourceVersion(long sourceVersion)
    {
        if (sourceVersion < 0)
        {
            throw new ReadingDomainException(
                "READING.INVALID_SEO_METADATA_SOURCE_VERSION",
                "SEO metadata source version must be non-negative.");
        }
    }

    private static void ValidateIncomingSourceVersion(long sourceVersion)
    {
        if (sourceVersion <= 0)
        {
            throw new ReadingDomainException(
                "READING.INVALID_INCOMING_SEO_METADATA_SOURCE_VERSION",
                "Incoming SEO metadata source version must be greater than zero.");
        }
    }

    private static void ValidateMessageId(string? messageId)
    {
        if (string.IsNullOrWhiteSpace(messageId))
        {
            return;
        }

        if (messageId.Trim().Length != PublicIdLength)
        {
            throw new ReadingDomainException(
                "READING.INVALID_SEO_METADATA_MESSAGE_ID",
                "SEO metadata message id must be a 26-character value.");
        }
    }

    private static void ValidateLastSyncedAtUtc(DateTime lastSyncedAtUtc)
    {
        if (lastSyncedAtUtc == default)
        {
            throw new ReadingDomainException(
                "READING.INVALID_LAST_SYNCED_AT_UTC",
                "Last synced timestamp is required.");
        }
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}