using Reading.Domain.Exceptions;

namespace Reading.Domain.Entities;

public sealed class ArticleReadModelTag
{
    private ArticleReadModelTag()
    {
    }

    public long ArticleId { get; private set; }

    public long TagId { get; private set; }

    public string? TagPublicId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Slug { get; private set; }

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
        ValidateSourceVersion(sourceVersion);

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
        ValidateSourceVersion(sourceVersion);

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
        if (!string.IsNullOrWhiteSpace(tagPublicId)
            && tagPublicId.Trim().Length != 26)
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
    }

    private static void ValidateSourceVersion(long sourceVersion)
    {
        if (sourceVersion < 0)
        {
            throw new ReadingDomainException(
                "READING.INVALID_SOURCE_VERSION",
                "Source version must be non-negative.");
        }
    }

    private static string? NormalizeNullable(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}