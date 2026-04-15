using System.Data;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using Identity.Application.Ports.Persistence;
using Identity.Domain.Entities;
using Identity.Infrastructure.Persistence.Exceptions;
using Identity.Infrastructure.Persistence.Sql;
using Microsoft.Data.SqlClient;

namespace Identity.Infrastructure.Persistence.Repositories
{
    public sealed class EmailVerificationTokenRepository : IEmailVerificationTokenRepository
    {
        private const string EmailVerificationTokenInsertProc = "[identity].[EmailVerificationToken_Insert]";
        private const string EmailVerificationTokenSelectActiveByTokenHashProc = "[identity].[EmailVerificationToken_SelectActiveByTokenHash]";
        private const string EmailVerificationTokenMarkUsedProc = "[identity].[EmailVerificationToken_MarkUsed]";

        private readonly IdentityUnitOfWork _unitOfWork;
        private readonly ISqlConnectionFactory _sqlConnectionFactory;
        private readonly IdentitySqlExceptionTranslator _sqlExceptionTranslator;

        public EmailVerificationTokenRepository(
            IdentityUnitOfWork unitOfWork,
            ISqlConnectionFactory sqlConnectionFactory,
            IdentitySqlExceptionTranslator sqlExceptionTranslator)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
            _sqlExceptionTranslator = sqlExceptionTranslator ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
        }

        public async Task InsertAsync(
            EmailVerificationToken token,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(token);

            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateCommandAsync(EmailVerificationTokenInsertProc, cancellationToken);

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

                    SqlParameter verificationTokenIdParameter = new("@VerificationTokenId", SqlDbType.BigInt)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(verificationTokenIdParameter);

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

        public async Task<EmailVerificationToken?> GetActiveByTokenHashAsync(
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
                    await CreateCommandAsync(EmailVerificationTokenSelectActiveByTokenHashProc, cancellationToken);

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

                    return MapEmailVerificationToken(reader);
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
            long verificationTokenId,
            CancellationToken cancellationToken = default)
        {
            if (verificationTokenId <= 0)
            {
                return false;
            }

            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateCommandAsync(EmailVerificationTokenMarkUsedProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.Add(
                        new SqlParameter("@VerificationTokenId", SqlDbType.BigInt)
                        {
                            Value = verificationTokenId
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

        private static EmailVerificationToken MapEmailVerificationToken(SqlDataReader reader)
        {
            return EmailVerificationToken.Rehydrate(
                verificationTokenId: reader.GetInt64(reader.GetOrdinal("VerificationTokenId")),
                userId: reader.GetInt64(reader.GetOrdinal("UserId")),
                tokenHash: (byte[])reader["TokenHash"],
                createdAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                expiresAt: reader.GetDateTime(reader.GetOrdinal("ExpiresAt")),
                usedAt: ReadNullableDateTime(reader, "UsedAt"),
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
}