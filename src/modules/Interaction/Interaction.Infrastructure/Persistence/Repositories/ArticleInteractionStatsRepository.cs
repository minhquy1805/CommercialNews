using System.Data;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using Interaction.Application.Models.Results;
using Interaction.Application.Ports.Persistence;
using Interaction.Infrastructure.Persistence.Exceptions;
using Microsoft.Data.SqlClient;
using ArticleInteractionStatsEntity = Interaction.Domain.Entities.ArticleInteractionStats;

namespace Interaction.Infrastructure.Persistence.Repositories;

public sealed class ArticleInteractionStatsRepository
    : IArticleInteractionStatsRepository
{
    private const string SelectByArticlePublicIdProc =
        "[interaction].[Interaction_ArticleInteractionStats_SelectByArticlePublicId]";

    private const string MaterializeProc =
        "[interaction].[Interaction_ArticleInteractionStats_Materialize]";

    private readonly IInteractionUnitOfWork _unitOfWork;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly InteractionSqlExceptionTranslator _sqlExceptionTranslator;

    public ArticleInteractionStatsRepository(
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

    public async Task<ArticleInteractionStatsEntity?> GetByArticlePublicIdAsync(
        string articlePublicId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(articlePublicId);

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(
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

    public async Task<MaterializeArticleInteractionStatsResult> MaterializeAsync(
        string articlePublicId,
        string publicationMessageIdCandidate,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(articlePublicId);
        ArgumentException.ThrowIfNullOrWhiteSpace(publicationMessageIdCandidate);

        try
        {
            /*
             * MaterializeArticleInteractionStatsUseCase opens the transaction
             * because:
             *
             * - ArticleInteractionStats materialization
             * - optional interaction.article_counters_projection_published outbox
             *
             * must commit atomically when SnapshotChanged = true.
             *
             * The same publicationMessageIdCandidate is persisted by this
             * procedure and later used as the Outbox MessageId.
             */
            using SqlCommand command = CreateTransactionalCommand(
                MaterializeProc);

            SqlParameter snapshotChangedParameter =
                CreateOutputBitParameter("@SnapshotChanged");

            command.Parameters.AddRange(
            [
                new SqlParameter("@ArticlePublicId", SqlDbType.Char, 26)
                {
                    Value = articlePublicId
                },
                new SqlParameter("@PublicationMessageIdCandidate", SqlDbType.Char, 26)
                {
                    Value = publicationMessageIdCandidate
                },
                snapshotChangedParameter
            ]);

            ArticleInteractionStatsEntity stats;

            using (SqlDataReader reader =
                   await command.ExecuteReaderAsync(cancellationToken))
            {
                if (!await reader.ReadAsync(cancellationToken))
                {
                    throw new InvalidOperationException(
                        "Interaction_ArticleInteractionStats_Materialize did not return an article interaction stats row.");
                }

                stats = MapArticleInteractionStats(reader);
            }

            /*
             * Output parameters are read after the reader has been disposed.
             */
            bool snapshotChanged = GetRequiredBoolean(
                snapshotChangedParameter,
                "Interaction_ArticleInteractionStats_Materialize did not return SnapshotChanged.");

            return new MaterializeArticleInteractionStatsResult(
                Stats: stats,
                SnapshotChanged: snapshotChanged);
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    private SqlCommand CreateTransactionalCommand(
        string storedProcedureName)
    {
        SqlCommand command = _unitOfWork.Connection.CreateCommand();

        command.Transaction = _unitOfWork.Transaction;
        command.CommandText = storedProcedureName;
        command.CommandType = CommandType.StoredProcedure;

        return command;
    }

    private async Task<(SqlCommand Command, SqlConnection? OwnedConnection)>
        CreateReadCommandAsync(
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

        SqlConnection ownedConnection =
            _sqlConnectionFactory.CreateConnection();

        await ownedConnection.OpenAsync(cancellationToken);

        SqlCommand command = ownedConnection.CreateCommand();

        command.CommandText = storedProcedureName;
        command.CommandType = CommandType.StoredProcedure;

        return (command, ownedConnection);
    }

    private static ArticleInteractionStatsEntity MapArticleInteractionStats(
        SqlDataReader reader)
    {
        return ArticleInteractionStatsEntity.Rehydrate(
            articleInteractionStatsId:
                reader.GetInt64(
                    reader.GetOrdinal("ArticleInteractionStatsId")),
            articlePublicId:
                reader.GetString(
                    reader.GetOrdinal("ArticlePublicId")),
            viewCount:
                reader.GetInt64(
                    reader.GetOrdinal("ViewCount")),
            likeCount:
                reader.GetInt64(
                    reader.GetOrdinal("LikeCount")),
            visibleCommentCount:
                reader.GetInt64(
                    reader.GetOrdinal("VisibleCommentCount")),
            statsVersion:
                reader.GetInt64(
                    reader.GetOrdinal("StatsVersion")),
            lastMaterializedAtUtc:
                GetNullableDateTime(reader, "LastMaterializedAtUtc"),
            lastPublishedMessageId:
                GetNullableString(reader, "LastPublishedMessageId"),
            lastPublishedAtUtc:
                GetNullableDateTime(reader, "LastPublishedAtUtc"),
            createdAtUtc:
                reader.GetDateTime(
                    reader.GetOrdinal("CreatedAtUtc")),
            updatedAtUtc:
                GetNullableDateTime(reader, "UpdatedAtUtc"));
    }

    private static SqlParameter CreateOutputBitParameter(
        string name)
    {
        return new SqlParameter(name, SqlDbType.Bit)
        {
            Direction = ParameterDirection.Output
        };
    }

    private static bool GetRequiredBoolean(
        SqlParameter parameter,
        string errorMessage)
    {
        if (parameter.Value is null or DBNull)
        {
            throw new InvalidOperationException(errorMessage);
        }

        return Convert.ToBoolean(parameter.Value);
    }

    private static string? GetNullableString(
        SqlDataReader reader,
        string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);

        return reader.IsDBNull(ordinal)
            ? null
            : reader.GetString(ordinal);
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