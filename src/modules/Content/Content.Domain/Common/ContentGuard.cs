using Content.Domain.Exceptions;

namespace Content.Domain.Common;

public static class ContentGuard
{
    public static void AgainstInvalidId(
        long id,
        string code,
        string message)
    {
        if (id <= 0)
        {
            throw new ContentDomainException(code, message);
        }
    }

    public static void AgainstInvalidOptionalId(
        long? id,
        string code,
        string message)
    {
        if (id.HasValue && id.Value <= 0)
        {
            throw new ContentDomainException(code, message);
        }
    }

    public static void AgainstInvalidVersion(
        long version,
        string code,
        string message)
    {
        if (version <= 0)
        {
            throw new ContentDomainException(code, message);
        }
    }

    public static void AgainstInvalidOptionalVersion(
        long? version,
        string code,
        string message)
    {
        if (version.HasValue && version.Value <= 0)
        {
            throw new ContentDomainException(code, message);
        }
    }

    public static void AgainstRequiredText(
        string? value,
        string code,
        string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ContentDomainException(code, message);
        }
    }

    public static void AgainstTooLong(
        string? value,
        int maxLength,
        string code,
        string message)
    {
        if (value is not null && value.Trim().Length > maxLength)
        {
            throw new ContentDomainException(code, message);
        }
    }

    public static void AgainstUpdatedBeforeCreated(
        DateTime updatedAt,
        DateTime createdAt,
        string code,
        string message)
    {
        if (updatedAt < createdAt)
        {
            throw new ContentDomainException(code, message);
        }
    }

    public static void AgainstDeletedBeforeCreated(
        DateTime? deletedAt,
        DateTime createdAt,
        string code,
        string message)
    {
        if (deletedAt.HasValue && deletedAt.Value < createdAt)
        {
            throw new ContentDomainException(code, message);
        }
    }

    public static void AgainstDefaultDateTime(
        DateTime value,
        string code,
        string message)
    {
        if (value == default)
        {
            throw new ContentDomainException(code, message);
        }
    }
}
