using Reading.Domain.Exceptions;

namespace Reading.Domain.Entities;

public sealed class ArticleMediaProjectionState
{
    private const int PublicIdLength = 26;

    private ArticleMediaProjectionState()
    {
    }

    public long ArticleId { get; private set; }

    /// <summary>
    /// Version of the Media article attachment-set state.
    /// This is not the Content article version and not an individual MediaAsset version.
    /// </summary>
    public long SourceVersion { get; private set; }

    public string? LastEventMessageId { get; private set; }

    public DateTime? LastSourceOccurredAtUtc { get; private set; }

    public DateTime LastSyncedAtUtc { get; private set; }

    /// <summary>
    /// Creates an initial media projection checkpoint before the first
    /// Media attachment-set event is applied.
    /// </summary>
    public static ArticleMediaProjectionState CreateInitial(
        long articleId,
        DateTime lastSyncedAtUtc)
    {
        ValidateArticleId(articleId);
        ValidateLastSyncedAtUtc(lastSyncedAtUtc);

        return new ArticleMediaProjectionState
        {
            ArticleId = articleId,
            SourceVersion = 0,
            LastEventMessageId = null,
            LastSourceOccurredAtUtc = null,
            LastSyncedAtUtc = lastSyncedAtUtc
        };
    }

    /// <summary>
    /// Rehydrates an existing persisted projection state.
    /// SourceVersion may be zero for a newly initialized state.
    /// </summary>
    public static ArticleMediaProjectionState Create(
        long articleId,
        long sourceVersion,
        string? lastEventMessageId,
        DateTime? lastSourceOccurredAtUtc,
        DateTime lastSyncedAtUtc)
    {
        ValidateArticleId(articleId);
        ValidateStoredSourceVersion(sourceVersion);
        ValidateMessageId(lastEventMessageId);
        ValidateLastSyncedAtUtc(lastSyncedAtUtc);

        return new ArticleMediaProjectionState
        {
            ArticleId = articleId,
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
        long sourceVersion,
        string? messageId,
        DateTime? sourceOccurredAtUtc,
        DateTime lastSyncedAtUtc)
    {
        ValidateIncomingSourceVersion(sourceVersion);
        ValidateMessageId(messageId);
        ValidateLastSyncedAtUtc(lastSyncedAtUtc);

        if (!CanApply(sourceVersion))
        {
            return false;
        }

        SourceVersion = sourceVersion;
        LastEventMessageId = NormalizeNullable(messageId);
        LastSourceOccurredAtUtc = sourceOccurredAtUtc;
        LastSyncedAtUtc = lastSyncedAtUtc;

        return true;
    }

    private static void ValidateArticleId(long articleId)
    {
        if (articleId <= 0)
        {
            throw new ReadingDomainException(
                "READING.INVALID_ARTICLE_ID",
                "Article id must be greater than zero.");
        }
    }

    private static void ValidateStoredSourceVersion(long sourceVersion)
    {
        if (sourceVersion < 0)
        {
            throw new ReadingDomainException(
                "READING.INVALID_MEDIA_PROJECTION_SOURCE_VERSION",
                "Media projection source version must be non-negative.");
        }
    }

    private static void ValidateIncomingSourceVersion(long sourceVersion)
    {
        if (sourceVersion <= 0)
        {
            throw new ReadingDomainException(
                "READING.INVALID_INCOMING_MEDIA_SOURCE_VERSION",
                "Incoming media source version must be greater than zero.");
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
                "READING.INVALID_MEDIA_PROJECTION_MESSAGE_ID",
                "Media projection message id must be a 26-character value.");
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