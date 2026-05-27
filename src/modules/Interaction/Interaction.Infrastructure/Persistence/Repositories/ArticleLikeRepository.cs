using System.Data;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using Interaction.Application.Models.Results;
using Interaction.Application.Ports.Persistence;
using Interaction.Domain.Entities;
using Interaction.Infrastructure.Persistence.Exceptions;
using Microsoft.Data.SqlClient;

namespace Interaction.Infrastructure.Persistence.Repositories;

public sealed class ArticleLikeRepository : IArticleLikeRepository
{
    private const string SelectByArticlePublicIdAndUserIdProc =
        "[interaction].[Interaction_ArticleLike_SelectByArticlePublicIdAndUserId]";

    private const string SetLikedProc =
        "[interaction].[Interaction_ArticleLike_SetLiked]";

    private const string SetUnlikedProc =
        "[interaction].[Interaction_ArticleLike_SetUnliked]";

    private const string GetActiveCountByArticlePublicIdProc =
        "[interaction].[Interaction_ArticleLike_GetActiveCountByArticlePublicId]";

    private readonly IInteractionUnitOfWork _unitOfWork;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly InteractionSqlExceptionTranslator _sqlExceptionTranslator;

    public ArticleLikeRepository(
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

    public async Task<ArticleLike?> GetByArticlePublicIdAndUserIdAsync(
        string articlePublicId,
        long userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(articlePublicId);

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(
                    SelectByArticlePublicIdAndUserIdProc,
                    cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.AddRange(
                [
                    new SqlParameter("@ArticlePublicId", SqlDbType.Char, 26)
                    {
                        Value = articlePublicId
                    },
                    new SqlParameter("@UserId", SqlDbType.BigInt)
                    {
                        Value = userId
                    }
                ]);

                using SqlDataReader reader =
                    await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return MapArticleLike(reader);
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

    public async Task<ArticleLikeMutationResult> SetLikedAsync(
        string publicId,
        string articlePublicId,
        long userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(publicId);
        ArgumentException.ThrowIfNullOrWhiteSpace(articlePublicId);

        try
        {
            /*
             * This method must be called inside the transaction opened by
             * LikeArticleUseCase, because ArticleLike mutation and Outbox
             * publication must commit atomically.
             */
            using SqlCommand command = CreateTransactionalCommand(SetLikedProc);

            SqlParameter changedParameter = new("@Changed", SqlDbType.Bit)
            {
                Direction = ParameterDirection.Output
            };

            command.Parameters.AddRange(
            [
                new SqlParameter("@PublicId", SqlDbType.Char, 26)
                {
                    Value = publicId
                },
                new SqlParameter("@ArticlePublicId", SqlDbType.Char, 26)
                {
                    Value = articlePublicId
                },
                new SqlParameter("@UserId", SqlDbType.BigInt)
                {
                    Value = userId
                },
                changedParameter
            ]);

            ArticleLike? articleLike;

            using (SqlDataReader reader =
                   await command.ExecuteReaderAsync(cancellationToken))
            {
                articleLike = await reader.ReadAsync(cancellationToken)
                    ? MapArticleLike(reader)
                    : null;
            }

            return new ArticleLikeMutationResult(
                ArticleLike: articleLike,
                Changed: GetRequiredBoolean(
                    changedParameter,
                    "Interaction_ArticleLike_SetLiked did not return Changed."));
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<ArticleLikeMutationResult> SetUnlikedAsync(
        string articlePublicId,
        long userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(articlePublicId);

        try
        {
            /*
             * This method must be called inside the transaction opened by
             * UnlikeArticleUseCase, because an actual unlike transition and
             * its Outbox publication must commit atomically.
             */
            using SqlCommand command = CreateTransactionalCommand(SetUnlikedProc);

            SqlParameter changedParameter = new("@Changed", SqlDbType.Bit)
            {
                Direction = ParameterDirection.Output
            };

            command.Parameters.AddRange(
            [
                new SqlParameter("@ArticlePublicId", SqlDbType.Char, 26)
                {
                    Value = articlePublicId
                },
                new SqlParameter("@UserId", SqlDbType.BigInt)
                {
                    Value = userId
                },
                changedParameter
            ]);

            ArticleLike? articleLike;

            using (SqlDataReader reader =
                   await command.ExecuteReaderAsync(cancellationToken))
            {
                articleLike = await reader.ReadAsync(cancellationToken)
                    ? MapArticleLike(reader)
                    : null;
            }

            return new ArticleLikeMutationResult(
                ArticleLike: articleLike,
                Changed: GetRequiredBoolean(
                    changedParameter,
                    "Interaction_ArticleLike_SetUnliked did not return Changed."));
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<long> GetActiveCountByArticlePublicIdAsync(
        string articlePublicId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(articlePublicId);

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(
                    GetActiveCountByArticlePublicIdProc,
                    cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@ArticlePublicId", SqlDbType.Char, 26)
                    {
                        Value = articlePublicId
                    });

                object? scalar = await command.ExecuteScalarAsync(
                    cancellationToken);

                return scalar is null or DBNull
                    ? 0L
                    : Convert.ToInt64(scalar);
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

    private static ArticleLike MapArticleLike(SqlDataReader reader)
    {
        return ArticleLike.Rehydrate(
            articleLikeId:
                reader.GetInt64(
                    reader.GetOrdinal("ArticleLikeId")),
            publicId:
                reader.GetString(
                    reader.GetOrdinal("PublicId")),
            articlePublicId:
                reader.GetString(
                    reader.GetOrdinal("ArticlePublicId")),
            userId:
                reader.GetInt64(
                    reader.GetOrdinal("UserId")),
            isActive:
                reader.GetBoolean(
                    reader.GetOrdinal("IsActive")),
            likedAtUtc:
                reader.GetDateTime(
                    reader.GetOrdinal("LikedAtUtc")),
            unlikedAtUtc:
                GetNullableDateTime(reader, "UnlikedAtUtc"),
            version:
                reader.GetInt64(
                    reader.GetOrdinal("Version")),
            createdAtUtc:
                reader.GetDateTime(
                    reader.GetOrdinal("CreatedAtUtc")),
            updatedAtUtc:
                GetNullableDateTime(reader, "UpdatedAtUtc"));
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