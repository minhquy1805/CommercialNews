using System.Data;
using CommercialNews.BuildingBlocks.Persistence.Sql;
using Content.Application.Ports.Persistence;
using Content.Infrastructure.Persistence.Sql;
using Microsoft.Data.SqlClient;

namespace Content.Infrastructure.Persistence.Repositories
{
    public sealed class CategoryRepository : ICategoryRepository
    {
        private readonly ContentUnitOfWork _unitOfWork;
        private readonly ISqlConnectionFactory _sqlConnectionFactory;

        public CategoryRepository(
            ContentUnitOfWork unitOfWork,
            ISqlConnectionFactory sqlConnectionFactory)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
        }

        public async Task<bool> ExistsByIdAsync(
            long categoryId,
            CancellationToken cancellationToken = default)
        {
            if (categoryId <= 0)
            {
                return false;
            }

            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateReadCommandAsync("Content_Category_ExistsById", cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.Add(
                        new SqlParameter("@CategoryId", SqlDbType.BigInt) { Value = categoryId });

                    object? result = await command.ExecuteScalarAsync(cancellationToken);

                    if (result is null || result == DBNull.Value)
                    {
                        return false;
                    }

                    return Convert.ToInt32(result) > 0;
                }
            }
            finally
            {
                if (ownedConnection is not null)
                {
                    await ownedConnection.DisposeAsync();
                }
            }
        }

        private async Task<(SqlCommand Command, SqlConnection? OwnedConnection)> CreateReadCommandAsync(
            string storedProcedureName,
            CancellationToken cancellationToken)
        {
            if (_unitOfWork.HasActiveConnection)
            {
                SqlCommand ambientCommand = _unitOfWork.Connection.CreateCommand();
                ambientCommand.Transaction = _unitOfWork.HasActiveTransaction
                    ? _unitOfWork.Transaction
                    : null;
                ambientCommand.CommandText = storedProcedureName;
                ambientCommand.CommandType = CommandType.StoredProcedure;

                return (ambientCommand, null);
            }

            SqlConnection ownedConnection = _sqlConnectionFactory.CreateConnection();
            await ownedConnection.OpenAsync(cancellationToken);

            SqlCommand command = ownedConnection.CreateCommand();
            command.CommandText = storedProcedureName;
            command.CommandType = CommandType.StoredProcedure;

            return (command, ownedConnection);
        }
    }
}

