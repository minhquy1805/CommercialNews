using Reading.Domain.Constants;
using Reading.Domain.Exceptions;

namespace Reading.Domain.Entities;

public sealed class ArticleSeoRouteProjection
{
    private const int PublicIdLength = 26;
    private const int MaxSlugLength = 200;
    private const int MaxCanonicalUrlLength = 500;

    private ArticleSeoRouteProjection()
    {
    }

    public string Scope { get; private set; } = string.Empty;

    public string ResourceType { get; private set; } = string.Empty;

    public string ResourcePublicId { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public string? CanonicalUrl { get; private set; }

    public bool IsActive { get; private set; }

    public bool IsIndexable { get; private set; }

    /// <summary>
    /// Version of the SEO route / slug registry aggregate only.
    /// This is not the Content article version and not the SEO metadata version.
    /// </summary>
    public long SourceVersion { get; private set; }

    /// <summary>
    /// Message id of the latest applied SEO route event.
    /// </summary>
    public string? LastEventMessageId { get; private set; }

    public DateTime? LastSourceOccurredAtUtc { get; private set; }

    public DateTime LastSyncedAtUtc { get; private set; }

    /// <summary>
    /// Creates or rehydrates an SEO route projection row.
    /// Stored source version may be zero for an initialized or reconstructed state.
    /// </summary>
    public static ArticleSeoRouteProjection Create(
        string scope,
        string resourceType,
        string resourcePublicId,
        string slug,
        string? canonicalUrl,
        bool isActive,
        bool isIndexable,
        long sourceVersion,
        string? lastEventMessageId,
        DateTime? lastSourceOccurredAtUtc,
        DateTime lastSyncedAtUtc)
    {
        string normalizedScope = ValidateAndNormalizeScope(scope);
        string normalizedResourceType = ValidateAndNormalizeResourceType(resourceType);

        ValidateResourcePublicId(resourcePublicId);
        ValidateSlug(slug);
        ValidateCanonicalUrl(canonicalUrl);
        ValidateStoredSourceVersion(sourceVersion);
        ValidateMessageId(lastEventMessageId);
        ValidateLastSyncedAtUtc(lastSyncedAtUtc);

        return new ArticleSeoRouteProjection
        {
            Scope = normalizedScope,
            ResourceType = normalizedResourceType,
            ResourcePublicId = resourcePublicId.Trim(),

            Slug = slug.Trim(),
            CanonicalUrl = NormalizeNullable(canonicalUrl),

            IsActive = isActive,
            IsIndexable = NormalizeIsIndexable(isActive, isIndexable),

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
        string slug,
        string? canonicalUrl,
        bool isActive,
        bool isIndexable,
        long sourceVersion,
        string? messageId,
        DateTime? sourceOccurredAtUtc,
        DateTime lastSyncedAtUtc)
    {
        ValidateSlug(slug);
        ValidateCanonicalUrl(canonicalUrl);
        ValidateIncomingSourceVersion(sourceVersion);
        ValidateMessageId(messageId);
        ValidateLastSyncedAtUtc(lastSyncedAtUtc);

        if (!CanApply(sourceVersion))
        {
            return false;
        }

        Slug = slug.Trim();
        CanonicalUrl = NormalizeNullable(canonicalUrl);

        IsActive = isActive;
        IsIndexable = NormalizeIsIndexable(isActive, isIndexable);

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
                "READING.INVALID_SEO_ROUTE_SCOPE",
                "Only public SEO scope is supported by Reading article route projection.");
        }

        return normalized;
    }

    private static string ValidateAndNormalizeResourceType(string resourceType)
    {
        string? normalized = ReadingProjectionResourceTypes.NormalizeOrNull(resourceType);

        if (normalized is null)
        {
            throw new ReadingDomainException(
                "READING.INVALID_SEO_ROUTE_RESOURCE_TYPE",
                "Only Article SEO resource type is supported by Reading article route projection.");
        }

        return normalized;
    }

    private static void ValidateResourcePublicId(string resourcePublicId)
    {
        if (string.IsNullOrWhiteSpace(resourcePublicId)
            || resourcePublicId.Trim().Length != PublicIdLength)
        {
            throw new ReadingDomainException(
                "READING.INVALID_SEO_ROUTE_RESOURCE_PUBLIC_ID",
                "SEO route resource public id must be a 26-character value.");
        }
    }

    private static void ValidateSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ReadingDomainException(
                "READING.INVALID_SEO_ROUTE_SLUG",
                "SEO route slug is required.");
        }

        if (slug.Trim().Length > MaxSlugLength)
        {
            throw new ReadingDomainException(
                "READING.SEO_ROUTE_SLUG_TOO_LONG",
                $"SEO route slug must not exceed {MaxSlugLength} characters.");
        }
    }

    private static void ValidateCanonicalUrl(string? canonicalUrl)
    {
        if (string.IsNullOrWhiteSpace(canonicalUrl))
        {
            return;
        }

        if (canonicalUrl.Trim().Length > MaxCanonicalUrlLength)
        {
            throw new ReadingDomainException(
                "READING.SEO_ROUTE_CANONICAL_URL_TOO_LONG",
                $"SEO route canonical URL must not exceed {MaxCanonicalUrlLength} characters.");
        }
    }

    private static void ValidateStoredSourceVersion(long sourceVersion)
    {
        if (sourceVersion < 0)
        {
            throw new ReadingDomainException(
                "READING.INVALID_SEO_ROUTE_SOURCE_VERSION",
                "SEO route source version must be non-negative.");
        }
    }

    private static void ValidateIncomingSourceVersion(long sourceVersion)
    {
        if (sourceVersion <= 0)
        {
            throw new ReadingDomainException(
                "READING.INVALID_INCOMING_SEO_ROUTE_SOURCE_VERSION",
                "Incoming SEO route source version must be greater than zero.");
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
                "READING.INVALID_SEO_ROUTE_MESSAGE_ID",
                "SEO route message id must be a 26-character value.");
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

    private static bool NormalizeIsIndexable(
        bool isActive,
        bool isIndexable)
    {
        return isActive && isIndexable;
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}