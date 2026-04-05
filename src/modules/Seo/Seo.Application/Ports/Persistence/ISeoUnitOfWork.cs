namespace Seo.Application.Ports.Persistence;

public interface ISeoUnitOfWork
{
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}