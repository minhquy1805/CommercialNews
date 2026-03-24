namespace Authorization.Application.Contracts.Ports
{
    public interface IAuthorizationUnitOfWork
    {
        Task BeginTransactionAsync(CancellationToken cancellationToken);
        Task CommitAsync(CancellationToken cancellationToken);
        Task RollbackAsync(CancellationToken cancellationToken);
    }
}