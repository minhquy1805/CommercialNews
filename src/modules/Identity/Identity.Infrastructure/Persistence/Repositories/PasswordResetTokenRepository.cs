using System.Data;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using Identity.Application.Ports.Persistence;
using Identity.Domain.Entities;
using Identity.Infrastructure.Persistence.Exceptions;
using Identity.Infrastructure.Persistence.Sql;
using Microsoft.Data.SqlClient;

namespace Identity.Infrastructure.Persistence.Repositories;

public sealed class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private const string PasswordResetTokenRevokeActiveByUserIdProc = "[identity].[PasswordResetToken_RevokeActiveByUserId]";
    private const string PasswordResetTokenInsertProc = "[identity].[PasswordResetToken_Insert]";
    private const string PasswordResetTokenSelectActiveByTokenHashProc = "[identity].[PasswordResetToken_SelectActiveByTokenHash]";
    private const string PasswordResetTokenMarkUsedProc = "[identity].[PasswordResetToken_MarkUsed]";

    private readonly IdentityUnitOfWork _unitOfWork;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly IdentitySqlExceptionTranslator _sqlExceptionTranslator;

    public PasswordResetTokenRepository(
        IdentityUnitOfWork unitOfWork,
        ISqlConnectionFactory sqlConnectionFactory,
        IdentitySqlExceptionTranslator sqlExceptionTranslator)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
        _sqlExceptionTranslator = sqlExceptionTranslator ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
    }

    public async Task<int> RevokeActiveByUserIdAsync(
        long userId,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return 0;
        }

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateCommandAsync(PasswordResetTokenRevokeActiveByUserIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@UserId", SqlDbType.BigInt)
                    {
                        Value = userId
                    });

                command.Parameters.Add(
                    new SqlParameter("@RevokedAt", SqlDbType.DateTime2)
                    {
                        Value = revokedAtUtc
                    });

                SqlParameter affectedRowsParameter = new("@AffectedRows", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                command.Parameters.Add(affectedRowsParameter);

                await command.ExecuteNonQueryAsync(cancellationToken);

                return affectedRowsParameter.Value is DBNull
                    ? 0
                    : Convert.ToInt32(affectedRowsParameter.Value);
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

    public async Task<long> InsertAsync(
        PasswordResetToken token,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(token);

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateCommandAsync(PasswordResetTokenInsertProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@UserId", SqlDbType.BigInt)
                    {
                        Value = token.UserId
                    });

                command.Parameters.Add(
                    new SqlParameter("@TokenHash", SqlDbType.VarBinary, 32)
                    {
                        Value = token.TokenHash
                    });

                command.Parameters.Add(
                    new SqlParameter("@ExpiresAt", SqlDbType.DateTime2)
                    {
                        Value = token.ExpiresAt
                    });

                command.Parameters.Add(
                    new SqlParameter("@CreatedIp", SqlDbType.NVarChar, 45)
                    {
                        Value = (object?)token.CreatedIp ?? DBNull.Value
                    });

                command.Parameters.Add(
                    new SqlParameter("@CorrelationId", SqlDbType.NVarChar, 100)
                    {
                        Value = (object?)token.CorrelationId ?? DBNull.Value
                    });

                SqlParameter resetTokenIdParameter = new("@ResetTokenId", SqlDbType.BigInt)
                {
                    Direction = ParameterDirection.Output
                };

                command.Parameters.Add(resetTokenIdParameter);

                await command.ExecuteNonQueryAsync(cancellationToken);

                return resetTokenIdParameter.Value is DBNull
                    ? 0
                    : Convert.ToInt64(resetTokenIdParameter.Value);
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

    public async Task<PasswordResetToken?> GetActiveByTokenHashAsync(
        byte[] tokenHash,
        CancellationToken cancellationToken = default)
    {
        if (tokenHash is null || tokenHash.Length == 0)
        {
            return null;
        }

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateCommandAsync(PasswordResetTokenSelectActiveByTokenHashProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@TokenHash", SqlDbType.VarBinary, 32)
                    {
                        Value = tokenHash
                    });

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return MapPasswordResetToken(reader);
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

    public async Task<bool> MarkUsedAsync(
        long resetTokenId,
        DateTime usedAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (resetTokenId <= 0)
        {
            return false;
        }

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateCommandAsync(PasswordResetTokenMarkUsedProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@ResetTokenId", SqlDbType.BigInt)
                    {
                        Value = resetTokenId
                    });

                command.Parameters.Add(
                    new SqlParameter("@UsedAt", SqlDbType.DateTime2)
                    {
                        Value = usedAtUtc
                    });

                SqlParameter affectedRowsParameter = new("@AffectedRows", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                command.Parameters.Add(affectedRowsParameter);

                await command.ExecuteNonQueryAsync(cancellationToken);

                int affectedRows = affectedRowsParameter.Value is DBNull
                    ? 0
                    : Convert.ToInt32(affectedRowsParameter.Value);

                return affectedRows > 0;
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

    private static PasswordResetToken MapPasswordResetToken(SqlDataReader reader)
    {
        return PasswordResetToken.Rehydrate(
            resetTokenId: reader.GetInt64(reader.GetOrdinal("ResetTokenId")),
            userId: reader.GetInt64(reader.GetOrdinal("UserId")),
            tokenHash: (byte[])reader["TokenHash"],
            createdAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            expiresAt: reader.GetDateTime(reader.GetOrdinal("ExpiresAt")),
            usedAt: ReadNullableDateTime(reader, "UsedAt"),
            revokedAt: ReadNullableDateTime(reader, "RevokedAt"),
            createdIp: ReadNullableString(reader, "CreatedIp"),
            correlationId: ReadNullableString(reader, "CorrelationId"));
    }

    private static string? ReadNullableString(SqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static DateTime? ReadNullableDateTime(SqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }
}