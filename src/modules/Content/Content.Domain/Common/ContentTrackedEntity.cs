namespace Content.Domain.Common;

public abstract class ContentTrackedEntity : Entity
{
    public DateTime CreatedAt { get; protected set; }

    public DateTime UpdatedAt { get; protected set; }

    public long? CreatedByUserId { get; protected set; }

    public long? UpdatedByUserId { get; protected set; }

    public long Version { get; protected set; }

    protected void InitializeTracking(DateTime nowUtc, long? actorUserId)
    {
        CreatedAt = nowUtc;
        UpdatedAt = nowUtc;
        CreatedByUserId = actorUserId;
        UpdatedByUserId = actorUserId;
        Version = 1;
    }

    protected void RehydrateTracking(
        DateTime createdAt,
        DateTime updatedAt,
        long? createdByUserId,
        long? updatedByUserId,
        long version)
    {
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        CreatedByUserId = createdByUserId;
        UpdatedByUserId = updatedByUserId;
        Version = version;
    }

    protected void MarkUpdated(DateTime nowUtc, long? actorUserId)
    {
        UpdatedAt = nowUtc;
        UpdatedByUserId = actorUserId;
        Version++;
    }
}
