using System.Data;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using Content.Application.Ports.Persistence;
using Content.Domain.Entities;
using Content.Infrastructure.Persistence.Exceptions;
using Content.Infrastructure.Persistence.Sql;
using Microsoft.Data.SqlClient;

namespace Content.Infrastructure.Persistence.Repositories;

public sealed class ArticleLifecycleEventRepository : IArticleLifecycleEventRepository
{
    private const string ArticleLifecycleEventInsertProc = "[content].[Content_ArticleLifecycleEvent_Insert]";
    private const string ArticleLifecycleEventSelectByArticleIdProc =
        "[content].[Content_ArticleLifecycleEvent_SelectByArticleId]";

    private readonly ContentUnitOfWork _unitOfWork;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly ContentSqlExceptionTranslator _sqlExceptionTranslator;

    public ArticleLifecycleEventRepository(
        ContentUnitOfWork unitOfWork,
        ISqlConnectionFactory sqlConnectionFactory,
        ContentSqlExceptionTranslator sqlExceptionTranslator)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
        _sqlExceptionTranslator = sqlExceptionTranslator ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
    }

    public async Task<ArticleLifecycleEvent?> InsertAsync(
        ArticleLifecycleEvent lifecycleEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lifecycleEvent);

        try
        {
            using SqlCommand command = CreateTransactionalCommand(ArticleLifecycleEventInsertProc);

            command.Parameters.AddRange(
            [
                new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = lifecycleEvent.ArticleId },
                new SqlParameter("@ArticleVersion", SqlDbType.BigInt) { Value = lifecycleEvent.ArticleVersion },
                new SqlParameter("@ActionType", SqlDbType.NVarChar, 30) { Value = lifecycleEvent.ActionType },
                new SqlParameter("@FromStatus", SqlDbType.NVarChar, 30)
                {
                    Value = ToDbValue(lifecycleEvent.FromStatus)
                },
                new SqlParameter("@ToStatus", SqlDbType.NVarChar, 30)
                {
                    Value = ToDbValue(lifecycleEvent.ToStatus)
                },
                new SqlParameter("@Reason", SqlDbType.NVarChar, 500)
                {
                    Value = ToDbValue(lifecycleEvent.Reason)
                },
                new SqlParameter("@ActorUserId", SqlDbType.BigInt) { Value = lifecycleEvent.ActorUserId },
                new SqlParameter("@CorrelationId", SqlDbType.NVarChar, 100)
                {
                    Value = ToDbValue(lifecycleEvent.CorrelationId)
                },
                new SqlParameter("@MetadataJson", SqlDbType.NVarChar, -1)
                {
                    Value = ToDbValue(lifecycleEvent.MetadataJson)
                }
            ]);

            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return MapArticleLifecycleEvent(reader);
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<IReadOnlyList<ArticleLifecycleEvent>> GetByArticleIdAsync(
        long articleId,
        CancellationToken cancellationToken = default)
    {
        if (articleId <= 0)
        {
            return [];
        }

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateCommandAsync(ArticleLifecycleEventSelectByArticleIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = articleId });

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                List<ArticleLifecycleEvent> lifecycleEvents = [];

                while (await reader.ReadAsync(cancellationToken))
                {
                    lifecycleEvents.Add(MapArticleLifecycleEvent(reader));
                }

                return lifecycleEvents;
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

    private SqlCommand CreateTransactionalCommand(string storedProcedureName)
    {
        if (!_unitOfWork.HasActiveConnection || !_unitOfWork.HasActiveTransaction)
        {
            throw new InvalidOperationException(
                "Content write operation requires an active SQL transaction.");
        }

        SqlCommand command = _unitOfWork.Connection.CreateCommand();
        command.Transaction = _unitOfWork.Transaction;
        command.CommandText = storedProcedureName;
        command.CommandType = CommandType.StoredProcedure;
        return command;
    }

    private async Task<(SqlCommand Command, SqlConnection? OwnedConnection)> CreateCommandAsync(
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

    private static ArticleLifecycleEvent MapArticleLifecycleEvent(SqlDataReader reader)
    {
        return ArticleLifecycleEvent.Rehydrate(
            eventId: reader.GetInt64(reader.GetOrdinal("EventId")),
            articleId: reader.GetInt64(reader.GetOrdinal("ArticleId")),
            articleVersion: reader.GetInt64(reader.GetOrdinal("ArticleVersion")),
            actionType: reader.GetString(reader.GetOrdinal("ActionType")),
            fromStatus: GetNullableString(reader, "FromStatus"),
            toStatus: GetNullableString(reader, "ToStatus"),
            reason: GetNullableString(reader, "Reason"),
            actorUserId: reader.GetInt64(reader.GetOrdinal("ActorUserId")),
            occurredAt: reader.GetDateTime(reader.GetOrdinal("OccurredAt")),
            correlationId: GetNullableString(reader, "CorrelationId"),
            metadataJson: GetNullableString(reader, "MetadataJson"));
    }

    private static object ToDbValue(object? value) => value ?? DBNull.Value;

    private static string? GetNullableString(SqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}
