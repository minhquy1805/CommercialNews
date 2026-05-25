using Reading.Domain.Exceptions;

namespace Reading.Domain.Entities;

public sealed class AuthorProfileProjection
{
    private const int PublicIdLength = 26;
    private const int MessageIdLength = 26;

    private const int MaxAuthorDisplayNameLength = 200;
    private const int MaxAuthorAvatarUrlLength = 800;

    private AuthorProfileProjection()
    {
    }

    public long AuthorUserId { get; private set; }

    public string AuthorUserPublicId { get; private set; } = string.Empty;

    public string? AuthorDisplayName { get; private set; }

    public string? AuthorAvatarUrl { get; private set; }

    /// <summary>
    /// Version of the Identity.UserAccount source stream.
    /// This value must not be compared with Content, Media, SEO,
    /// or Interaction projection versions.
    /// </summary>
    public long SourceVersion { get; private set; }

    /// <summary>
    /// Message id of the latest applied Identity author-profile event.
    /// </summary>
    public string LastEventMessageId { get; private set; } = string.Empty;

    /// <summary>
    /// Timestamp at which the source Identity event occurred.
    /// </summary>
    public DateTime LastSourceOccurredAtUtc { get; private set; }

    /// <summary>
    /// Timestamp at which Reading last synchronized this projection.
    /// </summary>
    public DateTime LastSyncedAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Creates a new local author-profile projection from the first
    /// accepted Identity integration event.
    /// </summary>
    public static AuthorProfileProjection Create(
        long authorUserId,
        string authorUserPublicId,
        string? authorDisplayName,
        string? authorAvatarUrl,
        long sourceVersion,
        string lastEventMessageId,
        DateTime lastSourceOccurredAtUtc,
        DateTime syncedAtUtc)
    {
        ValidateAuthorUserId(authorUserId);
        ValidateAuthorUserPublicId(authorUserPublicId);
        ValidatePublicProfile(authorDisplayName, authorAvatarUrl);
        ValidateSourceVersion(sourceVersion);
        ValidateRequiredMessageId(lastEventMessageId);

        ValidateRequiredTimestamp(
            lastSourceOccurredAtUtc,
            "READING.INVALID_AUTHOR_PROFILE_SOURCE_OCCURRED_AT_UTC",
            "Author profile source occurred timestamp is required.");

        ValidateRequiredTimestamp(
            syncedAtUtc,
            "READING.INVALID_AUTHOR_PROFILE_LAST_SYNCED_AT_UTC",
            "Author profile last synced timestamp is required.");

        return new AuthorProfileProjection
        {
            AuthorUserId = authorUserId,
            AuthorUserPublicId = authorUserPublicId.Trim(),
            AuthorDisplayName = NormalizeNullable(authorDisplayName),
            AuthorAvatarUrl = NormalizeNullable(authorAvatarUrl),
            SourceVersion = sourceVersion,
            LastEventMessageId = lastEventMessageId.Trim(),
            LastSourceOccurredAtUtc = lastSourceOccurredAtUtc,
            LastSyncedAtUtc = syncedAtUtc,
            CreatedAtUtc = syncedAtUtc,
            UpdatedAtUtc = syncedAtUtc
        };
    }

    /// <summary>
    /// Rehydrates an existing local projection row loaded from persistence.
    /// </summary>
    public static AuthorProfileProjection Rehydrate(
        long authorUserId,
        string authorUserPublicId,
        string? authorDisplayName,
        string? authorAvatarUrl,
        long sourceVersion,
        string lastEventMessageId,
        DateTime lastSourceOccurredAtUtc,
        DateTime lastSyncedAtUtc,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        ValidateAuthorUserId(authorUserId);
        ValidateAuthorUserPublicId(authorUserPublicId);
        ValidatePublicProfile(authorDisplayName, authorAvatarUrl);
        ValidateSourceVersion(sourceVersion);
        ValidateRequiredMessageId(lastEventMessageId);

        ValidateRequiredTimestamp(
            lastSourceOccurredAtUtc,
            "READING.INVALID_AUTHOR_PROFILE_SOURCE_OCCURRED_AT_UTC",
            "Author profile source occurred timestamp is required.");

        ValidateRequiredTimestamp(
            lastSyncedAtUtc,
            "READING.INVALID_AUTHOR_PROFILE_LAST_SYNCED_AT_UTC",
            "Author profile last synced timestamp is required.");

        ValidateRequiredTimestamp(
            createdAtUtc,
            "READING.INVALID_AUTHOR_PROFILE_CREATED_AT_UTC",
            "Author profile created timestamp is required.");

        ValidateRequiredTimestamp(
            updatedAtUtc,
            "READING.INVALID_AUTHOR_PROFILE_UPDATED_AT_UTC",
            "Author profile updated timestamp is required.");

        return new AuthorProfileProjection
        {
            AuthorUserId = authorUserId,
            AuthorUserPublicId = authorUserPublicId.Trim(),
            AuthorDisplayName = NormalizeNullable(authorDisplayName),
            AuthorAvatarUrl = NormalizeNullable(authorAvatarUrl),
            SourceVersion = sourceVersion,
            LastEventMessageId = lastEventMessageId.Trim(),
            LastSourceOccurredAtUtc = lastSourceOccurredAtUtc,
            LastSyncedAtUtc = lastSyncedAtUtc,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = updatedAtUtc
        };
    }

    /// <summary>
    /// Determines whether an incoming Identity event is newer than the
    /// currently projected author-profile version.
    /// </summary>
    public bool CanApply(long incomingSourceVersion)
    {
        ValidateSourceVersion(incomingSourceVersion);

        return incomingSourceVersion > SourceVersion;
    }

    /// <summary>
    /// Applies a newer Identity author-profile event in memory.
    /// The database stored procedure remains responsible for atomic,
    /// version-aware persistence during normal event ingestion.
    /// </summary>
    public bool Apply(
        string authorUserPublicId,
        string? authorDisplayName,
        string? authorAvatarUrl,
        long sourceVersion,
        string messageId,
        DateTime sourceOccurredAtUtc,
        DateTime syncedAtUtc)
    {
        ValidateAuthorUserPublicId(authorUserPublicId);
        ValidatePublicProfile(authorDisplayName, authorAvatarUrl);
        ValidateSourceVersion(sourceVersion);
        ValidateRequiredMessageId(messageId);

        ValidateRequiredTimestamp(
            sourceOccurredAtUtc,
            "READING.INVALID_AUTHOR_PROFILE_SOURCE_OCCURRED_AT_UTC",
            "Author profile source occurred timestamp is required.");

        ValidateRequiredTimestamp(
            syncedAtUtc,
            "READING.INVALID_AUTHOR_PROFILE_LAST_SYNCED_AT_UTC",
            "Author profile last synced timestamp is required.");

        EnsureAuthorIdentityMatches(authorUserPublicId);

        if (!CanApply(sourceVersion))
        {
            return false;
        }

        AuthorDisplayName = NormalizeNullable(authorDisplayName);
        AuthorAvatarUrl = NormalizeNullable(authorAvatarUrl);
        SourceVersion = sourceVersion;
        LastEventMessageId = messageId.Trim();
        LastSourceOccurredAtUtc = sourceOccurredAtUtc;
        LastSyncedAtUtc = syncedAtUtc;
        UpdatedAtUtc = syncedAtUtc;

        return true;
    }

    private void EnsureAuthorIdentityMatches(string authorUserPublicId)
    {
        if (!string.Equals(
                AuthorUserPublicId,
                authorUserPublicId.Trim(),
                StringComparison.Ordinal))
        {
            throw new ReadingDomainException(
                "READING.AUTHOR_PROFILE_PUBLIC_ID_MISMATCH",
                "Author user public id cannot change for an existing author profile projection.");
        }
    }

    private static void ValidateAuthorUserId(long authorUserId)
    {
        if (authorUserId <= 0)
        {
            throw new ReadingDomainException(
                "READING.INVALID_AUTHOR_USER_ID",
                "Author user id must be greater than zero.");
        }
    }

    private static void ValidateAuthorUserPublicId(string? authorUserPublicId)
    {
        if (string.IsNullOrWhiteSpace(authorUserPublicId)
            || authorUserPublicId.Trim().Length != PublicIdLength)
        {
            throw new ReadingDomainException(
                "READING.INVALID_AUTHOR_USER_PUBLIC_ID",
                $"Author user public id must be exactly {PublicIdLength} characters.");
        }
    }

    private static void ValidatePublicProfile(
        string? authorDisplayName,
        string? authorAvatarUrl)
    {
        ValidateOptionalLength(
            authorDisplayName,
            MaxAuthorDisplayNameLength,
            "READING.AUTHOR_DISPLAY_NAME_TOO_LONG",
            "Author display name");

        ValidateOptionalLength(
            authorAvatarUrl,
            MaxAuthorAvatarUrlLength,
            "READING.AUTHOR_AVATAR_URL_TOO_LONG",
            "Author avatar URL");
    }

    private static void ValidateSourceVersion(long sourceVersion)
    {
        if (sourceVersion <= 0)
        {
            throw new ReadingDomainException(
                "READING.INVALID_AUTHOR_PROFILE_SOURCE_VERSION",
                "Author profile source version must be greater than zero.");
        }
    }

    private static void ValidateRequiredMessageId(string? messageId)
    {
        if (string.IsNullOrWhiteSpace(messageId)
            || messageId.Trim().Length != MessageIdLength)
        {
            throw new ReadingDomainException(
                "READING.INVALID_AUTHOR_PROFILE_MESSAGE_ID",
                $"Author profile message id must be exactly {MessageIdLength} characters.");
        }
    }

    private static void ValidateRequiredTimestamp(
        DateTime value,
        string errorCode,
        string errorMessage)
    {
        if (value == default)
        {
            throw new ReadingDomainException(
                errorCode,
                errorMessage);
        }
    }

    private static void ValidateOptionalLength(
        string? value,
        int maxLength,
        string errorCode,
        string fieldName)
    {
        if (!string.IsNullOrWhiteSpace(value)
            && value.Trim().Length > maxLength)
        {
            throw new ReadingDomainException(
                errorCode,
                $"{fieldName} must not exceed {maxLength} characters.");
        }
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}