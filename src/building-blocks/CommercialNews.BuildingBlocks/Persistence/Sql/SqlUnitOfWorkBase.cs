using Microsoft.Data.SqlClient;

namespace CommercialNews.BuildingBlocks.Persistence.Sql
{
    public abstract class SqlUnitOfWorkBase : ISqlUnitOfWork
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        private SqlConnection? _connection;
        private SqlTransaction? _transaction;

        protected SqlUnitOfWorkBase(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public bool HasActiveTransaction => _transaction is not null;

        public bool HasActiveConnection => _connection is not null;

        public SqlConnection Connection =>
            _connection ?? throw new InvalidOperationException(
                "Transaction has not been started. Call BeginTransactionAsync first.");

        public SqlTransaction Transaction =>
            _transaction ?? throw new InvalidOperationException(
                "Transaction has not been started. Call BeginTransactionAsync first.");

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction is not null)
            {
                throw new InvalidOperationException("A transaction is already active.");
            }

            _connection = _connectionFactory.CreateConnection();
            await _connection.OpenAsync(cancellationToken);
            _transaction = (SqlTransaction)await _connection.BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction is null)
            {
                throw new InvalidOperationException("No active transaction to commit.");
            }

            await _transaction.CommitAsync(cancellationToken);
            await DisposeAsync();
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction is null)
            {
                return;
            }

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

