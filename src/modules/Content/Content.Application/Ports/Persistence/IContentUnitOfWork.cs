namespace Content.Application.Ports.Persistence
{
    public interface IContentUnitOfWork
    {
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);

        Task CommitAsync(CancellationToken cancellationToken = default);

        Task RollbackAsync(CancellationToken cancellationToken = default);
    }
}