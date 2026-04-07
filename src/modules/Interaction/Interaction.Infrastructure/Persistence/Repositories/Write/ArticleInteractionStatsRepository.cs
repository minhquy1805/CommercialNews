using System.Data;
using CommercialNews.BuildingBlocks.Persistence.Sql;
using Interaction.Application.Ports.Persistence.Write;
using Interaction.Domain.Entities;
using Interaction.Infrastructure.Persistence.Exceptions;
using Interaction.Infrastructure.Persistence.Sql;
using Microsoft.Data.SqlClient;

namespace Interaction.Infrastructure.Persistence.Repositories.Write;

// This is the write-side repository for ArticleInteractionStats.
// It is not used much in the current sync-base phase,
// but it is important for future aggregation flows such as:
// - async counter updates
// - replay / rebuild
// - projection refresh
// - worker-side stats upsert
//
// Query-side counter reads should go through
// IArticleInteractionStatsQueryRepository instead.
public sealed class ArticleInteractionStatsRepository : IArticleInteractionStatsRepository
{
    private const string ArticleInteractionStatsSelectByArticleIdProc =
        "[interaction].[Interaction_ArticleInteractionStats_SelectByArticleId]";

    private const string ArticleInteractionStatsUpsertProc =
        "[interaction].[Interaction_ArticleInteractionStats_Upsert]";

    private readonly InteractionUnitOfWork _unitOfWork;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly InteractionSqlExceptionTranslator _sqlExceptionTranslator;

    public ArticleInteractionStatsRepository(
        InteractionUnitOfWork unitOfWork,
        ISqlConnectionFactory sqlConnectionFactory,
        InteractionSqlExceptionTranslator sqlExceptionTranslator)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
        _sqlExceptionTranslator = sqlExceptionTranslator ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
    }

    public async Task<ArticleInteractionStats?> GetByArticleIdAsync(
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

                return MapArticleInteractionStats(reader);
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

    public async Task UpsertAsync(
        ArticleInteractionStats stats,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stats);

        try
        {
            using SqlCommand command = CreateTransactionalCommand(ArticleInteractionStatsUpsertProc);

            command.Parameters.AddRange(
            [
                new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = stats.ArticleId },
                new SqlParameter("@ViewsTotal", SqlDbType.BigInt) { Value = stats.ViewsTotal },
                new SqlParameter("@LikesTotal", SqlDbType.BigInt) { Value = stats.LikesTotal },
                new SqlParameter("@CommentsTotal", SqlDbType.BigInt) { Value = stats.CommentsTotal },
                new SqlParameter("@PopularityScore", SqlDbType.Decimal)
                {
                    Precision = 18,
                    Scale = 4,
                    Value = stats.PopularityScore
                }
            ]);

            await command.ExecuteNonQueryAsync(cancellationToken);
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

    private static ArticleInteractionStats MapArticleInteractionStats(SqlDataReader reader)
    {
        return ArticleInteractionStats.Rehydrate(
            articleId: reader.GetInt64(reader.GetOrdinal("ArticleId")),
            viewsTotal: reader.GetInt64(reader.GetOrdinal("ViewsTotal")),
            likesTotal: reader.GetInt64(reader.GetOrdinal("LikesTotal")),
            commentsTotal: reader.GetInt64(reader.GetOrdinal("CommentsTotal")),
            popularityScore: reader.GetDecimal(reader.GetOrdinal("PopularityScore")),
            createdAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            updatedAt: GetNullableDateTime(reader, "UpdatedAt"),
            lastAggregatedAt: GetNullableDateTime(reader, "LastAggregatedAt"));
    }

    private static DateTime? GetNullableDateTime(SqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }
}