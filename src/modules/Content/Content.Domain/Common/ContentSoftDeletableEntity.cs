using Content.Domain.Exceptions;

namespace Content.Domain.Common;

public abstract class ContentSoftDeletableEntity : ContentTrackedEntity
{
    public bool IsDeleted { get; protected set; }

    public DateTime? DeletedAt { get; protected set; }

    public long? DeletedByUserId { get; protected set; }

    protected void InitializeDeletion()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedByUserId = null;
    }

    protected void RehydrateDeletion(
        bool isDeleted,
        DateTime? deletedAt,
        long? deletedByUserId)
    {
        IsDeleted = isDeleted;
        DeletedAt = deletedAt;
        DeletedByUserId = deletedByUserId;
    }

    protected void EnsureNotDeleted(string code, string message)
    {
        if (IsDeleted)
        {
            throw new ContentDomainException(code, message);
        }
    }

    protected void MarkDeleted(DateTime nowUtc, long? actorUserId)
    {
        IsDeleted = true;
        DeletedAt = nowUtc;
        DeletedByUserId = actorUserId;
        MarkUpdated(nowUtc, actorUserId);
    }

    protected void MarkRestored(DateTime nowUtc, long? actorUserId)
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedByUserId = null;
        MarkUpdated(nowUtc, actorUserId);
    }
}
