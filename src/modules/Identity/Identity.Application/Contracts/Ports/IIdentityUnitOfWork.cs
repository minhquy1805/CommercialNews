namespace Identity.Application.Contracts.Ports
{
    public interface IIdentityUnitOfWork
    {
        Task BeginTransactionAsync(CancellationToken cancellationToken);
        Task CommitAsync(CancellationToken cancellationToken);
        Task RollbackAsync(CancellationToken cancellationToken);
    }
}
