using System.Data;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using Interaction.Application.Ports.Persistence.Write;
using Interaction.Domain.Entities;
using Interaction.Infrastructure.Persistence.Exceptions;
using Interaction.Infrastructure.Persistence.Sql;
using Microsoft.Data.SqlClient;

namespace Interaction.Infrastructure.Persistence.Repositories.Write;

public sealed class ArticleViewEventRepository : IArticleViewEventRepository
{
    private const string ArticleViewEventInsertProc =
        "[interaction].[Interaction_ArticleViewEvent_Insert]";

    private const string ArticleViewEventDeleteBeforeDateProc =
        "[interaction].[Interaction_ArticleViewEvent_DeleteBeforeDate]";

    private readonly InteractionUnitOfWork _unitOfWork;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly InteractionSqlExceptionTranslator _sqlExceptionTranslator;

    public ArticleViewEventRepository(
        InteractionUnitOfWork unitOfWork,
        ISqlConnectionFactory sqlConnectionFactory,
        InteractionSqlExceptionTranslator sqlExceptionTranslator)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
        _sqlExceptionTranslator = sqlExceptionTranslator ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
    }

    public async Task<long> InsertAsync(
        ArticleViewEvent articleViewEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(articleViewEvent);

        try
        {
            using SqlCommand command = CreateTransactionalCommand(ArticleViewEventInsertProc);

            command.Parameters.AddRange(
            [
                new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = articleViewEvent.ArticleId },
                new SqlParameter("@UserId", SqlDbType.BigInt) { Value = ToDbValue(articleViewEvent.UserId) },
                new SqlParameter("@VisitorKey", SqlDbType.NVarChar, 100) { Value = ToDbValue(articleViewEvent.VisitorKey) },
                new SqlParameter("@IpAddress", SqlDbType.NVarChar, 64) { Value = ToDbValue(articleViewEvent.IpAddress) },
                new SqlParameter("@UserAgent", SqlDbType.NVarChar, 512) { Value = ToDbValue(articleViewEvent.UserAgent) }
            ]);

            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("Interaction_ArticleViewEvent_Insert did not return a row.");
            }

            return reader.GetInt64(reader.GetOrdinal("ArticleViewEventId"));
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<int> DeleteBeforeDateAsync(
        DateTime deleteBeforeUtc,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using SqlCommand command = CreateTransactionalCommand(ArticleViewEventDeleteBeforeDateProc);

            SqlParameter affectedRowsParameter = new("@AffectedRows", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            command.Parameters.AddRange(
            [
                new SqlParameter("@DeleteBeforeUtc", SqlDbType.DateTime2) { Value = deleteBeforeUtc },
                affectedRowsParameter
            ]);

            await command.ExecuteNonQueryAsync(cancellationToken);

            return affectedRowsParameter.Value is DBNull
                ? 0
                : Convert.ToInt32(affectedRowsParameter.Value);
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    private SqlCommand CreateTransactionalCommand(string storedProcedureName)
    {
        SqlCommand command = _unitOfWork.Connection.CreateCommand();
        command.Transaction = _unitOfWork.Transaction;
        command.CommandText = storedProcedureName;
        command.CommandType = CommandType.StoredProcedure;
        return command;
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

    private static object ToDbValue(object? value) => value ?? DBNull.Value;
}