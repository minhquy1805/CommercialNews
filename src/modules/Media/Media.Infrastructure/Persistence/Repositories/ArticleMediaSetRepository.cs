using System.Data;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using Media.Application.Ports.Persistence;
using Media.Domain.Entities;
using Media.Infrastructure.Persistence.Exceptions;
using Media.Infrastructure.Persistence.Sql;
using Microsoft.Data.SqlClient;

namespace Media.Infrastructure.Persistence.Repositories;

public sealed class ArticleMediaSetRepository : IArticleMediaSetRepository
{
    private const string ArticleMediaSetSelectByArticleIdProc =
        "[media].[Media_ArticleMediaSet_SelectByArticleId]";

    private readonly IMediaUnitOfWork _unitOfWork;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly MediaSqlExceptionTranslator _sqlExceptionTranslator;

    public ArticleMediaSetRepository(
        IMediaUnitOfWork unitOfWork,
        ISqlConnectionFactory sqlConnectionFactory,
        MediaSqlExceptionTranslator sqlExceptionTranslator)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
        _sqlExceptionTranslator = sqlExceptionTranslator ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
    }

    public async Task<ArticleMediaSet?> GetByArticleIdAsync(
        long articleId,
        CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(
                    ArticleMediaSetSelectByArticleIdProc,
                    cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@ArticleId", SqlDbType.BigInt)
                    {
                        Value = articleId
                    });

                using SqlDataReader reader =
                    await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return MapArticleMediaSet(reader);
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

    private static ArticleMediaSet MapArticleMediaSet(SqlDataReader reader)
    {
        return ArticleMediaSet.Rehydrate(
            articleId: reader.GetInt64(reader.GetOrdinal("ArticleId")),
            version: reader.GetInt32(reader.GetOrdinal("Version")),
            createdAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            createdBy: GetNullableInt64(reader, "CreatedBy"),
            updatedAt: reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
            updatedBy: GetNullableInt64(reader, "UpdatedBy"));
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