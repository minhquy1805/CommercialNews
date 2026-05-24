using Reading.Domain.Exceptions;

namespace Reading.Domain.Entities;

public sealed class ArticleReadModelTag
{
    private const int PublicIdLength = 26;
    private const int MaxNameLength = 150;
    private const int MaxSlugLength = 200;

    private ArticleReadModelTag()
    {
    }

    public long ArticleId { get; private set; }

    public long TagId { get; private set; }

    public string? TagPublicId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Slug { get; private set; }

    /// <summary>
    /// Content article snapshot version used to project the article's tags.
    /// </summary>
    public long SourceVersion { get; private set; }

    public DateTime LastSyncedAtUtc { get; private set; }

    public static ArticleReadModelTag Create(
        long articleId,
        long tagId,
        string? tagPublicId,
        string name,
        string? slug,
        long sourceVersion,
        DateTime lastSyncedAtUtc)
    {
        ValidateArticleId(articleId);
        ValidateTagId(tagId);
        ValidateTagPublicId(tagPublicId);
        ValidateName(name);
        ValidateSlug(slug);
        ValidateIncomingSourceVersion(sourceVersion);
        ValidateLastSyncedAtUtc(lastSyncedAtUtc);

        return new ArticleReadModelTag
        {
            ArticleId = articleId,
            TagId = tagId,
            TagPublicId = NormalizeNullable(tagPublicId),
            Name = name.Trim(),
            Slug = NormalizeNullable(slug),
            SourceVersion = sourceVersion,
            LastSyncedAtUtc = lastSyncedAtUtc
        };
    }

    public bool CanApply(long incomingSourceVersion)
    {
        ValidateIncomingSourceVersion(incomingSourceVersion);

        return incomingSourceVersion > SourceVersion;
    }

    public bool Apply(
        string? tagPublicId,
        string name,
        string? slug,
        long sourceVersion,
        DateTime lastSyncedAtUtc)
    {
        ValidateTagPublicId(tagPublicId);
        ValidateName(name);
        ValidateSlug(slug);
        ValidateIncomingSourceVersion(sourceVersion);
        ValidateLastSyncedAtUtc(lastSyncedAtUtc);

        if (!CanApply(sourceVersion))
        {
            return false;
        }

        TagPublicId = NormalizeNullable(tagPublicId);
        Name = name.Trim();
        Slug = NormalizeNullable(slug);
        SourceVersion = sourceVersion;
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

    private static void ValidateTagId(long tagId)
    {
        if (tagId <= 0)
        {
            throw new ReadingDomainException(
                "READING.INVALID_TAG_ID",
                "Tag id must be greater than zero.");
        }
    }

    private static void ValidateTagPublicId(string? tagPublicId)
    {
        if (string.IsNullOrWhiteSpace(tagPublicId))
        {
            return;
        }

        if (tagPublicId.Trim().Length != PublicIdLength)
        {
            throw new ReadingDomainException(
                "READING.INVALID_TAG_PUBLIC_ID",
                "Tag public id must be a 26-character value.");
        }
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ReadingDomainException(
                "READING.INVALID_TAG_NAME",
                "Tag name is required.");
        }

        if (name.Trim().Length > MaxNameLength)
        {
            throw new ReadingDomainException(
                "READING.TAG_NAME_TOO_LONG",
                $"Tag name must not exceed {MaxNameLength} characters.");
        }
    }

    private static void ValidateSlug(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return;
        }

        if (slug.Trim().Length > MaxSlugLength)
        {
            throw new ReadingDomainException(
                "READING.TAG_SLUG_TOO_LONG",
                $"Tag slug must not exceed {MaxSlugLength} characters.");
        }
    }

    private static void ValidateIncomingSourceVersion(long sourceVersion)
    {
        if (sourceVersion <= 0)
        {
            throw new ReadingDomainException(
                "READING.INVALID_SOURCE_VERSION",
                "Source version must be greater than zero.");
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
