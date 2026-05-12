using System.Data;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using Content.Application.Ports.Persistence;
using Content.Domain.Entities;
using Content.Infrastructure.Persistence.Exceptions;
using Content.Infrastructure.Persistence.Sql;
using Microsoft.Data.SqlClient;

namespace Content.Infrastructure.Persistence.Repositories;

public sealed class ArticleTagRepository : IArticleTagRepository
{
    private const string ArticleTagInsertProc = "[content].[Content_ArticleTag_Insert]";
    private const string ArticleTagDeleteByArticleIdAndTagIdProc =
        "[content].[Content_ArticleTag_DeleteByArticleIdAndTagId]";
    private const string ArticleTagDeleteAllByArticleIdProc =
        "[content].[Content_ArticleTag_DeleteAllByArticleId]";
    private const string ArticleTagSelectByArticleIdProc =
        "[content].[Content_ArticleTag_SelectByArticleId]";

    private readonly ContentUnitOfWork _unitOfWork;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly ContentSqlExceptionTranslator _sqlExceptionTranslator;

    public ArticleTagRepository(
        ContentUnitOfWork unitOfWork,
        ISqlConnectionFactory sqlConnectionFactory,
        ContentSqlExceptionTranslator sqlExceptionTranslator)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
        _sqlExceptionTranslator = sqlExceptionTranslator ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
    }

    public async Task<ArticleTag?> InsertAsync(
        ArticleTag articleTag,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(articleTag);

        try
        {
            using SqlCommand command = CreateTransactionalCommand(ArticleTagInsertProc);

            command.Parameters.AddRange(
            [
                new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = articleTag.ArticleId },
                new SqlParameter("@TagId", SqlDbType.BigInt) { Value = articleTag.TagId },
                new SqlParameter("@AttachedByUserId", SqlDbType.BigInt)
                {
                    Value = ToDbValue(articleTag.AttachedByUserId)
                }
            ]);

            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return MapArticleTag(reader);
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<bool> DeleteByArticleIdAndTagIdAsync(
        long articleId,
        long tagId,
        CancellationToken cancellationToken = default)
    {
        if (articleId <= 0 || tagId <= 0)
        {
            return false;
        }

        try
        {
            using SqlCommand command = CreateTransactionalCommand(ArticleTagDeleteByArticleIdAndTagIdProc);

            command.Parameters.AddRange(
            [
                new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = articleId },
                new SqlParameter("@TagId", SqlDbType.BigInt) { Value = tagId }
            ]);

            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return false;
            }

            return reader.GetInt32(reader.GetOrdinal("RowsAffected")) > 0;
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<int> DeleteAllByArticleIdAsync(
        long articleId,
        CancellationToken cancellationToken = default)
    {
        if (articleId <= 0)
        {
            return 0;
        }

        try
        {
            using SqlCommand command = CreateTransactionalCommand(ArticleTagDeleteAllByArticleIdProc);

            command.Parameters.Add(
                new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = articleId });

            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return 0;
            }

            return reader.GetInt32(reader.GetOrdinal("RowsAffected"));
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<IReadOnlyList<ArticleTag>> GetByArticleIdAsync(
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
                await CreateCommandAsync(ArticleTagSelectByArticleIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = articleId });

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                List<ArticleTag> articleTags = [];

                while (await reader.ReadAsync(cancellationToken))
                {
                    articleTags.Add(MapArticleTag(reader));
                }

                return articleTags;
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

    private static ArticleTag MapArticleTag(SqlDataReader reader)
    {
        return ArticleTag.Rehydrate(
            articleId: reader.GetInt64(reader.GetOrdinal("ArticleId")),
            tagId: reader.GetInt64(reader.GetOrdinal("TagId")),
            attachedAt: reader.GetDateTime(reader.GetOrdinal("AttachedAt")),
            attachedByUserId: GetNullableInt64(reader, "AttachedByUserId"));
    }

    private static object ToDbValue(object? value) => value ?? DBNull.Value;

    private static long? GetNullableInt64(SqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);
    }
}
