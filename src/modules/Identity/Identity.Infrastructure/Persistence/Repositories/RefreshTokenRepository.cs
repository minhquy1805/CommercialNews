using System.Data;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using Identity.Application.Ports.Persistence;
using Identity.Domain.Entities;
using Identity.Infrastructure.Persistence.Exceptions;
using Identity.Infrastructure.Persistence.Sql;
using Microsoft.Data.SqlClient;

namespace Identity.Infrastructure.Persistence.Repositories
{
    public sealed class RefreshTokenRepository : IRefreshTokenRepository
    {
        private const string RefreshTokenInsertProc = "[identity].[RefreshToken_Insert]";
        private const string RefreshTokenSelectActiveByTokenHashProc = "[identity].[RefreshToken_SelectActiveByTokenHash]";
        private const string RefreshTokenSelectByTokenHashProc = "[identity].[RefreshToken_SelectByTokenHash]";
        private const string RefreshTokenRevokeProc = "[identity].[RefreshToken_Revoke]";
        private const string RefreshTokenRevokeAllActiveByUserIdProc = "[identity].[RefreshToken_RevokeAllActiveByUserId]";
        private const string RefreshTokenRotateProc = "[identity].[RefreshToken_Rotate]";

        private readonly IdentityUnitOfWork _unitOfWork;
        private readonly ISqlConnectionFactory _sqlConnectionFactory;
        private readonly IdentitySqlExceptionTranslator _sqlExceptionTranslator;

        public RefreshTokenRepository(
            IdentityUnitOfWork unitOfWork,
            ISqlConnectionFactory sqlConnectionFactory,
            IdentitySqlExceptionTranslator sqlExceptionTranslator)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
            _sqlExceptionTranslator = sqlExceptionTranslator ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
        }

        public async Task InsertAsync(
            RefreshToken refreshToken,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(refreshToken);

            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateCommandAsync(RefreshTokenInsertProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.Add(
                        new SqlParameter("@UserId", SqlDbType.BigInt)
                        {
                            Value = refreshToken.UserId
                        });

                    command.Parameters.Add(
                        new SqlParameter("@TokenHash", SqlDbType.VarBinary, 32)
                        {
                            Value = refreshToken.TokenHash
                        });

                    command.Parameters.Add(
                        new SqlParameter("@ExpiresAt", SqlDbType.DateTime2)
                        {
                            Value = refreshToken.ExpiresAt
                        });

                    command.Parameters.Add(
                        new SqlParameter("@CreatedIp", SqlDbType.NVarChar, 45)
                        {
                            Value = (object?)refreshToken.CreatedIp ?? DBNull.Value
                        });

                    command.Parameters.Add(
                        new SqlParameter("@UserAgent", SqlDbType.NVarChar, 300)
                        {
                            Value = (object?)refreshToken.UserAgent ?? DBNull.Value
                        });

                    command.Parameters.Add(
                        new SqlParameter("@CorrelationId", SqlDbType.NVarChar, 100)
                        {
                            Value = (object?)refreshToken.CorrelationId ?? DBNull.Value
                        });

                    SqlParameter refreshTokenIdParameter = new("@RefreshTokenId", SqlDbType.BigInt)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(refreshTokenIdParameter);

                    await command.ExecuteNonQueryAsync(cancellationToken);
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

        public async Task<RefreshToken?> GetActiveByTokenHashAsync(
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
                    await CreateCommandAsync(RefreshTokenSelectActiveByTokenHashProc, cancellationToken);

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

                    return MapRefreshToken(reader);
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

        public async Task<RefreshToken?> GetByTokenHashAsync(
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
                    await CreateCommandAsync(RefreshTokenSelectByTokenHashProc, cancellationToken);

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

                    return MapRefreshToken(reader);
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

        public async Task<bool> RevokeAsync(
            long refreshTokenId,
            string? revokedReason,
            byte[]? replacedByTokenHash,
            CancellationToken cancellationToken = default)
        {
            if (refreshTokenId <= 0)
            {
                return false;
            }

            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateCommandAsync(RefreshTokenRevokeProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.Add(
                        new SqlParameter("@RefreshTokenId", SqlDbType.BigInt)
                        {
                            Value = refreshTokenId
                        });

                    command.Parameters.Add(
                        new SqlParameter("@RevokedReason", SqlDbType.NVarChar, 200)
                        {
                            Value = !string.IsNullOrWhiteSpace(revokedReason)
                                ? revokedReason.Trim()
                                : DBNull.Value
                        });

                    command.Parameters.Add(
                        new SqlParameter("@ReplacedByTokenHash", SqlDbType.VarBinary, 32)
                        {
                            Value = replacedByTokenHash is not null
                                ? replacedByTokenHash
                                : DBNull.Value
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

        public async Task<int> RevokeAllActiveByUserIdAsync(
            long userId,
            string? revokedReason,
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
                    await CreateCommandAsync(RefreshTokenRevokeAllActiveByUserIdProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.Add(
                        new SqlParameter("@UserId", SqlDbType.BigInt)
                        {
                            Value = userId
                        });

                    command.Parameters.Add(
                        new SqlParameter("@RevokedReason", SqlDbType.NVarChar, 200)
                        {
                            Value = !string.IsNullOrWhiteSpace(revokedReason)
                                ? revokedReason.Trim()
                                : DBNull.Value
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

                    return affectedRows;
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

        public async Task<long?> RotateAsync(
            byte[] currentTokenHash,
            byte[] newTokenHash,
            DateTime newExpiresAtUtc,
            string? createdIp,
            string? userAgent,
            string? correlationId,
            CancellationToken cancellationToken = default)
        {
            if (currentTokenHash is null || currentTokenHash.Length == 0)
            {
                return null;
            }

            if (newTokenHash is null || newTokenHash.Length == 0)
            {
                return null;
            }

            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateCommandAsync(RefreshTokenRotateProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.Add(
                        new SqlParameter("@CurrentTokenHash", SqlDbType.VarBinary, 32)
                        {
                            Value = currentTokenHash
                        });

                    command.Parameters.Add(
                        new SqlParameter("@NewTokenHash", SqlDbType.VarBinary, 32)
                        {
                            Value = newTokenHash
                        });

                    command.Parameters.Add(
                        new SqlParameter("@NewExpiresAt", SqlDbType.DateTime2)
                        {
                            Value = newExpiresAtUtc
                        });

                    command.Parameters.Add(
                        new SqlParameter("@CreatedIp", SqlDbType.NVarChar, 45)
                        {
                            Value = (object?)createdIp ?? DBNull.Value
                        });

                    command.Parameters.Add(
                        new SqlParameter("@UserAgent", SqlDbType.NVarChar, 300)
                        {
                            Value = (object?)userAgent ?? DBNull.Value
                        });

                    command.Parameters.Add(
                        new SqlParameter("@CorrelationId", SqlDbType.NVarChar, 100)
                        {
                            Value = (object?)correlationId ?? DBNull.Value
                        });

                    SqlParameter newRefreshTokenIdParameter = new("@NewRefreshTokenId", SqlDbType.BigInt)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(newRefreshTokenIdParameter);

                    SqlParameter userIdParameter = new("@UserId", SqlDbType.BigInt)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(userIdParameter);

                    await command.ExecuteNonQueryAsync(cancellationToken);

                    if (userIdParameter.Value is DBNull)
                    {
                        return null;
                    }

                    return Convert.ToInt64(userIdParameter.Value);
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

        private static RefreshToken MapRefreshToken(SqlDataReader reader)
        {
            return RefreshToken.Rehydrate(
                refreshTokenId: reader.GetInt64(reader.GetOrdinal("RefreshTokenId")),
                userId: reader.GetInt64(reader.GetOrdinal("UserId")),
                tokenHash: (byte[])reader["TokenHash"],
                createdAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                expiresAt: reader.GetDateTime(reader.GetOrdinal("ExpiresAt")),
                revokedAt: ReadNullableDateTime(reader, "RevokedAt"),
                revokedReason: ReadNullableString(reader, "RevokedReason"),
                replacedByTokenHash: ReadNullableBytes(reader, "ReplacedByTokenHash"),
                createdIp: ReadNullableString(reader, "CreatedIp"),
                userAgent: ReadNullableString(reader, "UserAgent"),
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

        private static byte[]? ReadNullableBytes(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : (byte[])reader[columnName];
        }
    }
}