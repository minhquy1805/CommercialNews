using System.Data;
using CommercialNews.BuildingBlocks.Persistence.Sql;
using Interaction.Application.Models.QueryModels;
using Interaction.Application.Ports.Persistence.Read;
using Interaction.Infrastructure.Persistence.Exceptions;
using Interaction.Infrastructure.Persistence.Sql;
using Microsoft.Data.SqlClient;

namespace Interaction.Infrastructure.Persistence.Repositories.Read;

public sealed class ArticleInteractionStatsQueryRepository : IArticleInteractionStatsQueryRepository
{
    private const string ArticleInteractionStatsSelectByArticleIdProc =
        "[interaction].[Interaction_ArticleInteractionStats_SelectByArticleId]";

    private readonly InteractionUnitOfWork _unitOfWork;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly InteractionSqlExceptionTranslator _sqlExceptionTranslator;

    public ArticleInteractionStatsQueryRepository(
        InteractionUnitOfWork unitOfWork,
        ISqlConnectionFactory sqlConnectionFactory,
        InteractionSqlExceptionTranslator sqlExceptionTranslator)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
        _sqlExceptionTranslator = sqlExceptionTranslator ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
    }

    public async Task<ArticleCountersResult?> GetCountersByArticleIdAsync(
        long articleId,
        CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(ArticleInteractionStatsSelectByArticleIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = articleId });

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return new ArticleCountersResult
                {
                    ArticleId = reader.GetInt64(reader.GetOrdinal("ArticleId")),
                    Views = reader.GetInt64(reader.GetOrdinal("ViewsTotal")),
                    Likes = reader.GetInt64(reader.GetOrdinal("LikesTotal")),
                    Comments = reader.GetInt64(reader.GetOrdinal("CommentsTotal")),
                    Partial = false
                };
            }
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
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