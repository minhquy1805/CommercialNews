using Microsoft.Data.SqlClient;

namespace CommercialNews.BuildingBlocks.Persistence.Sql
{
    public interface ISqlUnitOfWork : IAsyncDisposable
    {
        bool HasActiveTransaction { get; }

        bool HasActiveConnection { get; }

        SqlConnection Connection { get; }

        SqlTransaction Transaction { get; }

        Task BeginTransactionAsync(CancellationToken cancellationToken = default);

        Task CommitAsync(CancellationToken cancellationToken = default);

        Task RollbackAsync(CancellationToken cancellationToken = default);
    }
}

