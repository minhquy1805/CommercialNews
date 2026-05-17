using Seo.Domain.Constants;
using Seo.Domain.Exceptions;

namespace Seo.Domain.Entities;

public sealed class SlugRegistry
{
    public long SlugId { get; private set; }

    public string Scope { get; private set; } = default!;
    public string Slug { get; private set; } = default!;

    public string ResourceType { get; private set; } = default!;
    public string ResourcePublicId { get; private set; } = default!;

    public string? CanonicalUrl { get; private set; }

    public bool IsIndexable { get; private set; }
    public bool IsActive { get; private set; }

    public long? SourceAggregateVersion { get; private set; }
    public string? LastAppliedMessageId { get; private set; }
    public DateTime? LastSyncedAtUtc { get; private set; }

    public int Version { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public long? CreatedByUserId { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }
    public long? UpdatedByUserId { get; private set; }

    private SlugRegistry()
    {
    }

    public static SlugRegistry Create(
        string? scope,
        string slug,
        string resourceType,
        string resourcePublicId,
        string? canonicalUrl,
        bool isIndexable,
        bool isActive,
        DateTime nowUtc,
        long? actorUserId)
    {
        var normalizedScope = NormalizeScope(scope);

        ValidateSlug(slug);
        ValidateResourceType(resourceType);
        ValidateResourcePublicId(resourcePublicId);
        ValidateCanonicalUrl(canonicalUrl);

        return new SlugRegistry
        {
            Scope = normalizedScope,
            Slug = NormalizeRequired(slug),
            ResourceType = resourceType.Trim(),
            ResourcePublicId = resourcePublicId.Trim(),
            CanonicalUrl = NormalizeOptional(canonicalUrl),
            IsIndexable = isIndexable,
            IsActive = isActive,
            Version = 1,
            CreatedAtUtc = nowUtc,
            CreatedByUserId = actorUserId,
            UpdatedAtUtc = nowUtc,
            UpdatedByUserId = actorUserId
        };
    }

    public static SlugRegistry Rehydrate(
        long slugId,
        string scope,
        string slug,
        string resourceType,
        string resourcePublicId,
        string? canonicalUrl,
        bool isIndexable,
        bool isActive,
        long? sourceAggregateVersion,
        string? lastAppliedMessageId,
        DateTime? lastSyncedAtUtc,
        int version,
        DateTime createdAtUtc,
        long? createdByUserId,
        DateTime updatedAtUtc,
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

        if (updatedAtUtc < createdAtUtc)
        {
            throw new SeoDomainException(
                "SEO.SLUG_REGISTRY_INVALID_UPDATED_AT",
                "Updated time is invalid for the current slug route state.");
        }

        ValidateScope(scope);
        ValidateSlug(slug);
        ValidateResourceType(resourceType);
        ValidateResourcePublicId(resourcePublicId);
        ValidateCanonicalUrl(canonicalUrl);
        ValidateSourceAggregateVersion(sourceAggregateVersion);
        ValidateLastAppliedMessageId(lastAppliedMessageId);

        return new SlugRegistry
        {
            SlugId = slugId,
            Scope = NormalizeRequired(scope),
            Slug = NormalizeRequired(slug),
            ResourceType = resourceType.Trim(),
            ResourcePublicId = resourcePublicId.Trim(),
            CanonicalUrl = NormalizeOptional(canonicalUrl),
            IsIndexable = isIndexable,
            IsActive = isActive,
            SourceAggregateVersion = sourceAggregateVersion,
            LastAppliedMessageId = NormalizeOptional(lastAppliedMessageId),
            LastSyncedAtUtc = lastSyncedAtUtc,
            Version = version,
            CreatedAtUtc = createdAtUtc,
            CreatedByUserId = createdByUserId,
            UpdatedAtUtc = updatedAtUtc,
            UpdatedByUserId = updatedByUserId
        };
    }

    public void UpdateRouting(
        string slug,
        string? scope,
        string resourceType,
        string resourcePublicId,
        string? canonicalUrl,
        bool isIndexable,
        bool isActive,
        DateTime nowUtc,
        long? actorUserId)
    {
        var normalizedScope = NormalizeScope(scope);

        ValidateSlug(slug);
        ValidateResourceType(resourceType);
        ValidateResourcePublicId(resourcePublicId);
        ValidateCanonicalUrl(canonicalUrl);

        Scope = normalizedScope;
        Slug = NormalizeRequired(slug);
        ResourceType = resourceType.Trim();
        ResourcePublicId = resourcePublicId.Trim();
        CanonicalUrl = NormalizeOptional(canonicalUrl);
        IsIndexable = isIndexable;
        IsActive = isActive;
        UpdatedAtUtc = nowUtc;
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
        UpdatedAtUtc = nowUtc;
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
        UpdatedAtUtc = nowUtc;
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
        UpdatedAtUtc = nowUtc;
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
        UpdatedAtUtc = nowUtc;
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
        UpdatedAtUtc = nowUtc;
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
        IsIndexable = false;
        UpdatedAtUtc = nowUtc;
        UpdatedByUserId = actorUserId;
        Version++;
    }

    public void ApplyContentVisibility(
        string? slug,
        string? canonicalUrl,
        bool isIndexable,
        bool isActive,
        long sourceAggregateVersion,
        string lastAppliedMessageId,
        DateTime lastSyncedAtUtc,
        DateTime nowUtc)
    {
        ValidateSourceAggregateVersion(sourceAggregateVersion);
        ValidateLastAppliedMessageId(lastAppliedMessageId);

        if (isActive)
        {
            ValidateSlug(slug ?? Slug);
        }

        ValidateCanonicalUrl(canonicalUrl);

        if (SourceAggregateVersion is not null &&
            sourceAggregateVersion <= SourceAggregateVersion.Value)
        {
            throw new SeoDomainException(
                "SEO.EVENT_STALE_IGNORED",
                "The incoming content event is stale and cannot be applied.");
        }

        if (!string.IsNullOrWhiteSpace(slug))
        {
            Slug = NormalizeRequired(slug);
        }

        CanonicalUrl = NormalizeOptional(canonicalUrl);
        IsIndexable = isIndexable;
        IsActive = isActive;
        SourceAggregateVersion = sourceAggregateVersion;
        LastAppliedMessageId = lastAppliedMessageId.Trim();
        LastSyncedAtUtc = lastSyncedAtUtc;
        UpdatedAtUtc = nowUtc;
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

    private static void ValidateSlug(string? slug)
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

    private static void ValidateCanonicalUrl(string? canonicalUrl)
    {
        if (!string.IsNullOrWhiteSpace(canonicalUrl) && canonicalUrl.Trim().Length > 500)
        {
            throw new SeoDomainException(
                "SEO.CANONICAL_URL_TOO_LONG",
                "Canonical URL must not exceed 500 characters.");
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
        if (string.IsNullOrWhiteSpace(lastAppliedMessageId))
        {
            return;
        }

        if (lastAppliedMessageId.Trim().Length != 26)
        {
            throw new SeoDomainException(
                "SEO.INVALID_LAST_APPLIED_MESSAGE_ID",
                "Last applied message id must be 26 characters.");
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