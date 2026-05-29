using System.Data;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using Interaction.Application.Models.Results;
using Interaction.Application.Ports.Persistence;
using Interaction.Domain.Entities;
using Interaction.Infrastructure.Persistence.Exceptions;
using Microsoft.Data.SqlClient;

namespace Interaction.Infrastructure.Persistence.Repositories;

public sealed class ArticleViewCountRepository : IArticleViewCountRepository
{
    private const string SelectByArticlePublicIdProc =
        "[interaction].[Interaction_ArticleViewCount_SelectByArticlePublicId]";

    private const string IncrementAcceptedProc =
        "[interaction].[Interaction_ArticleViewCount_IncrementAccepted]";

    private const string SelectPendingStatsMaterializationBatchProc =
        "[interaction].[Interaction_ArticleViewCount_SelectPendingStatsMaterializationBatch]";

    private readonly IInteractionUnitOfWork _unitOfWork;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly InteractionSqlExceptionTranslator _sqlExceptionTranslator;

    public ArticleViewCountRepository(
        IInteractionUnitOfWork unitOfWork,
        ISqlConnectionFactory sqlConnectionFactory,
        InteractionSqlExceptionTranslator sqlExceptionTranslator)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));

        _sqlConnectionFactory = sqlConnectionFactory
            ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));

        _sqlExceptionTranslator = sqlExceptionTranslator
            ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
    }

    public async Task<ArticleViewCount?> GetByArticlePublicIdAsync(
        string articlePublicId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(articlePublicId);

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateCommandAsync(
                    SelectByArticlePublicIdProc,
                    cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@ArticlePublicId", SqlDbType.Char, 26)
                    {
                        Value = articlePublicId
                    });

                using SqlDataReader reader =
                    await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return MapArticleViewCount(reader);
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

    public async Task<ArticleViewCount> IncrementAcceptedAsync(
        string articlePublicId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(articlePublicId);

        SqlConnection? ownedConnection = null;

        try
        {
            /*
             * TrackArticleView does not publish one outbox event per view.
             * Therefore this mutation can execute as one standalone atomic
             * stored-procedure call when no ambient transaction exists.
             */
            (SqlCommand command, SqlConnection? connection) =
                await CreateCommandAsync(
                    IncrementAcceptedProc,
                    cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@ArticlePublicId", SqlDbType.Char, 26)
                    {
                        Value = articlePublicId
                    });

                using SqlDataReader reader =
                    await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    throw new InvalidOperationException(
                        "Interaction_ArticleViewCount_IncrementAccepted did not return an article view count row.");
                }

                return MapArticleViewCount(reader);
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

    public async Task<IReadOnlyList<PendingViewStatsMaterializationItemResult>>
        GetPendingStatsMaterializationBatchAsync(
            int batchSize,
            CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateCommandAsync(
                    SelectPendingStatsMaterializationBatchProc,
                    cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@BatchSize", SqlDbType.Int)
                    {
                        Value = batchSize
                    });

                using SqlDataReader reader =
                    await command.ExecuteReaderAsync(cancellationToken);

                var pendingItems =
                    new List<PendingViewStatsMaterializationItemResult>();

                while (await reader.ReadAsync(cancellationToken))
                {
                    pendingItems.Add(
                        new PendingViewStatsMaterializationItemResult(
                            ArticlePublicId: reader.GetString(
                                reader.GetOrdinal("ArticlePublicId"))));
                }

                return pendingItems;
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

    private async Task<(SqlCommand Command, SqlConnection? OwnedConnection)>
        CreateCommandAsync(
            string storedProcedureName,
            CancellationToken cancellationToken)
    {
        if (_unitOfWork.HasActiveConnection)
        {
            SqlCommand ambientCommand = CreateCommand(
                _unitOfWork.Connection,
                _unitOfWork.HasActiveTransaction
                    ? _unitOfWork.Transaction
                    : null,
                storedProcedureName);

            return (ambientCommand, null);
        }

        SqlConnection ownedConnection =
            _sqlConnectionFactory.CreateConnection();

        await ownedConnection.OpenAsync(cancellationToken);

        SqlCommand command = CreateCommand(
            ownedConnection,
            transaction: null,
            storedProcedureName);

        return (command, ownedConnection);
    }

    private static SqlCommand CreateCommand(
        SqlConnection connection,
        SqlTransaction? transaction,
        string storedProcedureName)
    {
        SqlCommand command = connection.CreateCommand();

        command.Transaction = transaction;
        command.CommandText = storedProcedureName;
        command.CommandType = CommandType.StoredProcedure;

        return command;
    }

    private static ArticleViewCount MapArticleViewCount(
        SqlDataReader reader)
    {
        return ArticleViewCount.Rehydrate(
            articleViewCountId:
                reader.GetInt64(
                    reader.GetOrdinal("ArticleViewCountId")),
            articlePublicId:
                reader.GetString(
                    reader.GetOrdinal("ArticlePublicId")),
            viewCount:
                reader.GetInt64(
                    reader.GetOrdinal("ViewCount")),
            viewVersion:
                reader.GetInt64(
                    reader.GetOrdinal("ViewVersion")),
            lastAcceptedViewAtUtc:
                GetNullableDateTime(reader, "LastAcceptedViewAtUtc"),
            createdAtUtc:
                reader.GetDateTime(
                    reader.GetOrdinal("CreatedAtUtc")),
            updatedAtUtc:
                GetNullableDateTime(reader, "UpdatedAtUtc"));
    }

    private static DateTime? GetNullableDateTime(
        SqlDataReader reader,
        string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);

        return reader.IsDBNull(ordinal)
            ? null
            : reader.GetDateTime(ordinal);
    }
}