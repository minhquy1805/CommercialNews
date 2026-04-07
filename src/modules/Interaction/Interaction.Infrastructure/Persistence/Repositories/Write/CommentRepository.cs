using System.Data;
using CommercialNews.BuildingBlocks.Persistence.Sql;
using Interaction.Application.Ports.Persistence.Write;
using Interaction.Domain.Entities;
using Interaction.Infrastructure.Persistence.Exceptions;
using Interaction.Infrastructure.Persistence.Sql;
using Microsoft.Data.SqlClient;

namespace Interaction.Infrastructure.Persistence.Repositories.Write;

public sealed class CommentRepository : ICommentRepository
{
    private const string CommentInsertProc =
        "[interaction].[Interaction_Comment_Insert]";

    private const string CommentSelectByIdProc =
        "[interaction].[Interaction_Comment_SelectById]";

    private const string CommentUpdateProc =
        "[interaction].[Interaction_Comment_Update]";

    private const string CommentSoftDeleteProc =
        "[interaction].[Interaction_Comment_SoftDelete]";

    private readonly InteractionUnitOfWork _unitOfWork;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly InteractionSqlExceptionTranslator _sqlExceptionTranslator;

    public CommentRepository(
        InteractionUnitOfWork unitOfWork,
        ISqlConnectionFactory sqlConnectionFactory,
        InteractionSqlExceptionTranslator sqlExceptionTranslator)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
        _sqlExceptionTranslator = sqlExceptionTranslator ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
    }

    public async Task<long> InsertAsync(
        Comment comment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comment);

        try
        {
            using SqlCommand command = CreateTransactionalCommand(CommentInsertProc);

            command.Parameters.AddRange(
            [
                new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = comment.ArticleId },
                new SqlParameter("@UserId", SqlDbType.BigInt) { Value = comment.UserId },
                new SqlParameter("@ParentCommentId", SqlDbType.BigInt) { Value = ToDbValue(comment.ParentCommentId) },
                new SqlParameter("@Content", SqlDbType.NVarChar, 2000) { Value = comment.Content }
            ]);

            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("Interaction_Comment_Insert did not return a row.");
            }

            return reader.GetInt64(reader.GetOrdinal("CommentId"));
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<Comment?> GetByIdAsync(
        long commentId,
        CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(CommentSelectByIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@CommentId", SqlDbType.BigInt) { Value = commentId });

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return MapComment(reader);
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

    public async Task<int> UpdateAsync(
        Comment comment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comment);

        try
        {
            using SqlCommand command = CreateTransactionalCommand(CommentUpdateProc);

            SqlParameter affectedRowsParameter = new("@AffectedRows", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            command.Parameters.AddRange(
            [
                new SqlParameter("@CommentId", SqlDbType.BigInt) { Value = comment.CommentId },
                new SqlParameter("@Content", SqlDbType.NVarChar, 2000) { Value = comment.Content },
                new SqlParameter("@ExpectedUserId", SqlDbType.BigInt) { Value = comment.UserId },
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

    public async Task<int> SoftDeleteAsync(
        long commentId,
        long? deletedByUserId,
        long? expectedUserId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using SqlCommand command = CreateTransactionalCommand(CommentSoftDeleteProc);

            SqlParameter affectedRowsParameter = new("@AffectedRows", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            command.Parameters.AddRange(
            [
                new SqlParameter("@CommentId", SqlDbType.BigInt) { Value = commentId },
                new SqlParameter("@DeletedByUserId", SqlDbType.BigInt) { Value = ToDbValue(deletedByUserId) },
                new SqlParameter("@ExpectedUserId", SqlDbType.BigInt) { Value = ToDbValue(expectedUserId) },
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

    private static Comment MapComment(SqlDataReader reader)
    {
        return Comment.Rehydrate(
            commentId: reader.GetInt64(reader.GetOrdinal("CommentId")),
            articleId: reader.GetInt64(reader.GetOrdinal("ArticleId")),
            userId: reader.GetInt64(reader.GetOrdinal("UserId")),
            parentCommentId: GetNullableInt64(reader, "ParentCommentId"),
            content: reader.GetString(reader.GetOrdinal("Content")),
            status: reader.GetString(reader.GetOrdinal("Status")),
            createdAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            updatedAt: GetNullableDateTime(reader, "UpdatedAt"),
            deletedAt: GetNullableDateTime(reader, "DeletedAt"),
            deletedByUserId: GetNullableInt64(reader, "DeletedByUserId"),
            editCount: reader.GetInt32(reader.GetOrdinal("EditCount")));
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