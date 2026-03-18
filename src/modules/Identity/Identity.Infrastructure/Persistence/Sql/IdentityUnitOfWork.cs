using Identity.Application.Contracts.Ports;
using Microsoft.Data.SqlClient;

namespace Identity.Infrastructure.Persistence.Sql
{
    public sealed class IdentityUnitOfWork : IIdentityUnitOfWork, IAsyncDisposable
    {
        private readonly IdentitySqlConnectionFactory _connectionFactory;

        private SqlConnection? _connection;
        private SqlTransaction? _transaction;

        public IdentityUnitOfWork(IdentitySqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public bool HasActiveTransaction => _transaction is not null;
        public bool HasActiveConnection => _connection is not null;

        public SqlConnection Connection
        {
            get
            {
                if (_connection is null)
                    throw new InvalidOperationException("Transaction has not been started. Call BeginTransactionAsync first.");

                return _connection;
            }
        }

        public SqlTransaction Transaction
        {
            get
            {
                if (_transaction is null)
                    throw new InvalidOperationException("Transaction has not been started. Call BeginTransactionAsync first.");

                return _transaction;
            }
        }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken)
        {
            if (_transaction is not null)
                throw new InvalidOperationException("A transaction is already active.");

            _connection = _connectionFactory.CreateConnection();
            await _connection.OpenAsync(cancellationToken);
            _transaction = (SqlTransaction)await _connection.BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitAsync(CancellationToken cancellationToken)
        {
            if (_transaction is null)
                throw new InvalidOperationException("No active transaction to commit.");

            await _transaction.CommitAsync(cancellationToken);
            await DisposeAsync();
        }

        public async Task RollbackAsync(CancellationToken cancellationToken)
        {
            if (_transaction is null)
                return;

            await _transaction.RollbackAsync(cancellationToken);
            await DisposeAsync();
        }

        public async ValueTask DisposeAsync()
        {
            if (_transaction is not null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }

            if (_connection is not null)
            {
                await _connection.DisposeAsync();
                _connection = null;
            }
        }
    }
}
