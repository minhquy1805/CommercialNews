using System.Data;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using Content.Application.Ports.Persistence;
using Content.Domain.Entities;
using Content.Infrastructure.Persistence.Exceptions;
using Content.Infrastructure.Persistence.Sql;
using Microsoft.Data.SqlClient;

namespace Content.Infrastructure.Persistence.Repositories;

public sealed class ArticleRevisionRepository : IArticleRevisionRepository
{
    private const string ArticleRevisionInsertProc = "[content].[Content_ArticleRevision_Insert]";
    private const string ArticleRevisionSelectByArticleIdProc = "[content].[Content_ArticleRevision_SelectByArticleId]";
    private const string ArticleRevisionSelectByIdProc = "[content].[Content_ArticleRevision_SelectById]";

    private readonly ContentUnitOfWork _unitOfWork;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly ContentSqlExceptionTranslator _sqlExceptionTranslator;

    public ArticleRevisionRepository(
        ContentUnitOfWork unitOfWork,
        ISqlConnectionFactory sqlConnectionFactory,
        ContentSqlExceptionTranslator sqlExceptionTranslator)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
        _sqlExceptionTranslator = sqlExceptionTranslator ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
    }

    public async Task<ArticleRevision?> InsertAsync(
        ArticleRevision revision,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(revision);

        try
        {
            using SqlCommand command = CreateTransactionalCommand(ArticleRevisionInsertProc);

            command.Parameters.AddRange(
            [
                new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = revision.ArticleId },
                new SqlParameter("@EditedByUserId", SqlDbType.BigInt) { Value = revision.EditedByUserId },
                new SqlParameter("@ArticleVersion", SqlDbType.BigInt)
                {
                    Value = ToDbValue(revision.ArticleVersion)
                },
                new SqlParameter("@CorrelationId", SqlDbType.NVarChar, 100)
                {
                    Value = ToDbValue(revision.CorrelationId)
                },
                new SqlParameter("@ChangeSummary", SqlDbType.NVarChar, 300)
                {
                    Value = ToDbValue(revision.ChangeSummary)
                },
                new SqlParameter("@OldTitle", SqlDbType.NVarChar, 300)
                {
                    Value = ToDbValue(revision.OldTitle)
                },
                new SqlParameter("@OldSummary", SqlDbType.NVarChar, 1000)
                {
                    Value = ToDbValue(revision.OldSummary)
                },
                new SqlParameter("@OldBody", SqlDbType.NVarChar, -1)
                {
                    Value = ToDbValue(revision.OldBody)
                }
            ]);

            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return MapArticleRevision(reader);
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<IReadOnlyList<ArticleRevision>> GetByArticleIdAsync(
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
                await CreateCommandAsync(ArticleRevisionSelectByArticleIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = articleId });

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                List<ArticleRevision> revisions = [];

                while (await reader.ReadAsync(cancellationToken))
                {
                    revisions.Add(MapArticleRevision(reader));
                }

                return revisions;
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

    public async Task<ArticleRevision?> GetByIdAsync(
        long articleId,
        long revisionId,
        CancellationToken cancellationToken = default)
    {
        if (articleId <= 0 || revisionId <= 0)
        {
            return null;
        }

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateCommandAsync(ArticleRevisionSelectByIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.AddRange(
                [
                    new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = articleId },
                    new SqlParameter("@RevisionId", SqlDbType.BigInt) { Value = revisionId }
                ]);

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return MapArticleRevision(reader);
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

    private static ArticleRevision MapArticleRevision(SqlDataReader reader)
    {
        return ArticleRevision.Rehydrate(
            revisionId: reader.GetInt64(reader.GetOrdinal("RevisionId")),
            articleId: reader.GetInt64(reader.GetOrdinal("ArticleId")),
            editedAt: reader.GetDateTime(reader.GetOrdinal("EditedAt")),
            editedByUserId: reader.GetInt64(reader.GetOrdinal("EditedByUserId")),
            articleVersion: GetNullableInt64(reader, "ArticleVersion"),
            correlationId: GetNullableString(reader, "CorrelationId"),
            changeSummary: GetNullableString(reader, "ChangeSummary"),
            oldTitle: GetNullableString(reader, "OldTitle"),
            oldSummary: GetNullableString(reader, "OldSummary"),
            oldBody: GetNullableString(reader, "OldBody"));
    }

    private static object ToDbValue(object? value) => value ?? DBNull.Value;

    private static long? GetNullableInt64(SqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);
    }

    private static string? GetNullableString(SqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}
