namespace Content.Application.Ports.Persistence
{
    public interface IArticleLifecycleEventRepository
    {
        Task InsertAsync(
            long articleId,
            string actionType,
            string? fromStatus,
            string? toStatus,
            string? reason,
            DateTime occurredAt,
            long? actorUserId,
            CancellationToken cancellationToken = default);
    }
}