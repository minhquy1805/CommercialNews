using System.Data;
using CommercialNews.BuildingBlocks.Contracts.Common;
using CommercialNews.BuildingBlocks.Persistence.Sql;
using Interaction.Application.Models.QueryModels;
using Interaction.Application.Ports.Persistence.Read;
using Interaction.Infrastructure.Persistence.Exceptions;
using Interaction.Infrastructure.Persistence.Sql;
using Microsoft.Data.SqlClient;

namespace Interaction.Infrastructure.Persistence.Repositories.Read;

public sealed class CommentQueryRepository : ICommentQueryRepository
{
    private const string CommentSelectVisibleByArticleIdProc =
        "[interaction].[Interaction_Comment_SelectVisibleByArticleId]";

    private const string CommentGetVisibleCountByArticleIdProc =
        "[interaction].[Interaction_Comment_GetVisibleCountByArticleId]";

    private readonly InteractionUnitOfWork _unitOfWork;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly InteractionSqlExceptionTranslator _sqlExceptionTranslator;

    public CommentQueryRepository(
        InteractionUnitOfWork unitOfWork,
        ISqlConnectionFactory sqlConnectionFactory,
        InteractionSqlExceptionTranslator sqlExceptionTranslator)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
        _sqlExceptionTranslator = sqlExceptionTranslator ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
    }

    public async Task<PagedQueryResult<CommentListItem>> SelectVisibleByArticleIdAsync(
        CommentListQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(CommentSelectVisibleByArticleIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                int page = query.Page <= 0 ? 1 : query.Page;
                int pageSize = query.PageSize <= 0 ? 20 : query.PageSize;

                int skip = (page - 1) * pageSize;
                int take = pageSize;

                command.Parameters.AddRange(
                [
                    new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = query.ArticleId },
                    new SqlParameter("@Skip", SqlDbType.Int) { Value = skip },
                    new SqlParameter("@Take", SqlDbType.Int) { Value = take },
                    new SqlParameter("@ParentCommentId", SqlDbType.BigInt) { Value = ToDbValue(query.ParentCommentId) },
                    new SqlParameter("@SortBy", SqlDbType.NVarChar, 30) { Value = query.SortBy },
                    new SqlParameter("@SortDirection", SqlDbType.NVarChar, 4) { Value = query.SortDirection }
                ]);

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                List<CommentListItem> items = [];
                int totalItems = 0;

                while (await reader.ReadAsync(cancellationToken))
                {
                    if (totalItems == 0)
                    {
                        totalItems = reader.GetInt32(reader.GetOrdinal("TotalCount"));
                    }

                    items.Add(new CommentListItem
                    {
                        CommentId = reader.GetInt64(reader.GetOrdinal("CommentId")),
                        ArticleId = reader.GetInt64(reader.GetOrdinal("ArticleId")),
                        UserId = reader.GetInt64(reader.GetOrdinal("UserId")),
                        ParentCommentId = GetNullableInt64(reader, "ParentCommentId"),
                        Content = reader.GetString(reader.GetOrdinal("Content")),
                        Status = reader.GetString(reader.GetOrdinal("Status")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        UpdatedAt = GetNullableDateTime(reader, "UpdatedAt"),
                        EditCount = reader.GetInt32(reader.GetOrdinal("EditCount"))
                    });
                }

                return new PagedQueryResult<CommentListItem>
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

    public async Task<long> GetVisibleCountByArticleIdAsync(
        long articleId,
        CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(CommentGetVisibleCountByArticleIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = articleId });

                object? scalar = await command.ExecuteScalarAsync(cancellationToken);

                if (scalar is null || scalar is DBNull)
                {
                    return 0;
                }

                return Convert.ToInt64(scalar);
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

    private static object ToDbValue(object? value) => value ?? DBNull.Value;

    private static long? GetNullableInt64(SqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);
    }

    private static DateTime? GetNullableDateTime(SqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }
}