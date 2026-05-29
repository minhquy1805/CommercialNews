using System.Data;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using Interaction.Application.Models.Queries;
using Interaction.Application.Models.Results;
using Interaction.Application.Ports.Persistence;
using Interaction.Infrastructure.Persistence.Exceptions;
using Microsoft.Data.SqlClient;

namespace Interaction.Infrastructure.Persistence.Repositories;

public sealed class CommentModerationActionHistoryRepository
    : ICommentModerationActionHistoryRepository
{
    private const string SelectByCommentPublicIdProc =
        "[interaction].[Interaction_CommentModerationActionHistory_SelectByCommentPublicId]";

    private readonly IInteractionUnitOfWork _unitOfWork;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly InteractionSqlExceptionTranslator _sqlExceptionTranslator;

    public CommentModerationActionHistoryRepository(
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

    public async Task<PagedQueryResult<CommentModerationHistoryItemResult>>
        GetByCommentPublicIdAsync(
            GetCommentModerationHistoryQuery query,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(
                    SelectByCommentPublicIdProc,
                    cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                int page = query.Page <= 0 ? 1 : query.Page;
                int pageSize = NormalizePageSize(query.PageSize);
                int skip = checked((page - 1) * pageSize);

                command.Parameters.AddRange(
                [
                    new SqlParameter("@CommentPublicId", SqlDbType.Char, 26)
                    {
                        Value = query.CommentPublicId
                    },
                    new SqlParameter("@Skip", SqlDbType.Int)
                    {
                        Value = skip
                    },
                    new SqlParameter("@Take", SqlDbType.Int)
                    {
                        Value = pageSize
                    }
                ]);

                List<CommentModerationHistoryItemResult> items = [];
                int totalItems = 0;

                using SqlDataReader reader =
                    await command.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    items.Add(MapHistoryItem(reader));

                    totalItems = GetCountAsInt(
                        reader,
                        "TotalCount");
                }

                return new PagedQueryResult<CommentModerationHistoryItemResult>
                {
                    Items = items,
                    Page = page,
                    PageSize = pageSize,
                    TotalItems = totalItems
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

    private static CommentModerationHistoryItemResult MapHistoryItem(
        SqlDataReader reader)
    {
        return new CommentModerationHistoryItemResult(
            HistoryPublicId:
                reader.GetString(
                    reader.GetOrdinal("HistoryPublicId")),
            CommentPublicId:
                reader.GetString(
                    reader.GetOrdinal("CommentPublicId")),
            CommentModerationCasePublicId:
                GetNullableString(
                    reader,
                    "CommentModerationCasePublicId"),
            ActionType:
                reader.GetString(
                    reader.GetOrdinal("ActionType")),
            FromStatus:
                GetNullableString(
                    reader,
                    "FromStatus"),
            ToStatus:
                GetNullableString(
                    reader,
                    "ToStatus"),
            ActorUserId:
                GetNullableInt64(
                    reader,
                    "ActorUserId"),
            ActorType:
                reader.GetString(
                    reader.GetOrdinal("ActorType")),
            ReasonCode:
                GetNullableString(
                    reader,
                    "ReasonCode"),
            Note:
                GetNullableString(
                    reader,
                    "Note"),
            OccurredAtUtc:
                reader.GetDateTime(
                    reader.GetOrdinal("OccurredAtUtc")),
            CorrelationId:
                GetNullableString(
                    reader,
                    "CorrelationId"));
    }

    private static int NormalizePageSize(int pageSize)
    {
        if (pageSize <= 0)
        {
            return 20;
        }

        return pageSize > 200
            ? 200
            : pageSize;
    }

    private static int GetCountAsInt(
        SqlDataReader reader,
        string columnName)
    {
        long count = reader.GetInt64(
            reader.GetOrdinal(columnName));

        return checked((int)count);
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

    private static long? GetNullableInt64(
        SqlDataReader reader,
        string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);

        return reader.IsDBNull(ordinal)
            ? null
            : reader.GetInt64(ordinal);
    }
}