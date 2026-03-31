using System.Data;
using CommercialNews.BuildingBlocks.Persistence.Sql;
using Identity.Application.Ports.Persistence;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Infrastructure.Persistence.Exceptions;
using Identity.Infrastructure.Persistence.Sql;
using Microsoft.Data.SqlClient;

namespace Identity.Infrastructure.Persistence.Repositories
{
    public sealed class UserAccountRepository : IUserAccountRepository
    {
        private const string UserAccountInsertProc = "[identity].[UserAccount_Insert]";
        private const string UserAccountSelectByIdProc = "[identity].[UserAccount_SelectById]";
        private const string UserAccountSelectByPublicIdProc = "[identity].[UserAccount_SelectByPublicId]";
        private const string UserAccountSelectByEmailNormalizedProc = "[identity].[UserAccount_SelectByEmailNormalized]";
        private const string UserAccountUpdateProfileProc = "[identity].[UserAccount_UpdateProfile]";
        private const string UserAccountUpdatePasswordProc = "[identity].[UserAccount_UpdatePassword]";
        private const string UserAccountUpdateLastLoginProc = "[identity].[UserAccount_UpdateLastLogin]";
        private const string UserAccountSetEmailVerifiedProc = "[identity].[UserAccount_SetEmailVerified]";
        private const string UserAccountUpdateStatusProc = "[identity].[UserAccount_UpdateStatus]";

        private readonly IdentityUnitOfWork _unitOfWork;
        private readonly ISqlConnectionFactory _sqlConnectionFactory;
        private readonly IdentitySqlExceptionTranslator _sqlExceptionTranslator;

        public UserAccountRepository(
            IdentityUnitOfWork unitOfWork,
            ISqlConnectionFactory sqlConnectionFactory,
            IdentitySqlExceptionTranslator sqlExceptionTranslator)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
            _sqlExceptionTranslator = sqlExceptionTranslator ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
        }

        public async Task<UserAccount?> GetByIdAsync(
            long userId,
            CancellationToken cancellationToken = default)
        {
            if (userId <= 0)
            {
                return null;
            }

            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateCommandAsync(UserAccountSelectByIdProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.Add(
                        new SqlParameter("@UserId", SqlDbType.BigInt) { Value = userId });

                    using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                    if (!await reader.ReadAsync(cancellationToken))
                    {
                        return null;
                    }

                    return MapUserAccount(reader);
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

        public async Task<UserAccount?> GetByPublicIdAsync(
            string publicId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(publicId))
            {
                return null;
            }

            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateCommandAsync(UserAccountSelectByPublicIdProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.Add(
                        new SqlParameter("@PublicId", SqlDbType.Char, 26)
                        {
                            Value = publicId.Trim()
                        });

                    using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                    if (!await reader.ReadAsync(cancellationToken))
                    {
                        return null;
                    }

                    return MapUserAccount(reader);
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

        public async Task<UserAccount?> GetByEmailNormalizedAsync(
            string emailNormalized,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(emailNormalized))
            {
                return null;
            }

            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateCommandAsync(UserAccountSelectByEmailNormalizedProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.Add(
                        new SqlParameter("@EmailNormalized", SqlDbType.NVarChar, 320)
                        {
                            Value = emailNormalized.Trim()
                        });

                    using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                    if (!await reader.ReadAsync(cancellationToken))
                    {
                        return null;
                    }

                    return MapUserAccount(reader);
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

        public async Task<long> InsertAsync(
            UserAccount userAccount,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(userAccount);

            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateCommandAsync(UserAccountInsertProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.Add(
                        new SqlParameter("@PublicId", SqlDbType.Char, 26) { Value = userAccount.PublicId });

                    command.Parameters.Add(
                        new SqlParameter("@Email", SqlDbType.NVarChar, 320) { Value = userAccount.Email });

                    command.Parameters.Add(
                        new SqlParameter("@EmailNormalized", SqlDbType.NVarChar, 320) { Value = userAccount.EmailNormalized });

                    command.Parameters.Add(
                        new SqlParameter("@PasswordHash", SqlDbType.NVarChar, 500) { Value = userAccount.PasswordHash });

                    command.Parameters.Add(
                        new SqlParameter("@FullName", SqlDbType.NVarChar, 200)
                        {
                            Value = userAccount.FullName is not null
                                ? userAccount.FullName
                                : DBNull.Value
                        });

                    command.Parameters.Add(
                        new SqlParameter("@AvatarUrl", SqlDbType.NVarChar, 800)
                        {
                            Value = userAccount.AvatarUrl is not null
                                ? userAccount.AvatarUrl
                                : DBNull.Value
                        });

                    command.Parameters.Add(
                        new SqlParameter("@Status", SqlDbType.VarChar, 20)
                        {
                            Value = userAccount.Status.ToString()
                        });

                    SqlParameter userIdParameter = new("@UserId", SqlDbType.BigInt)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(userIdParameter);

                    await command.ExecuteNonQueryAsync(cancellationToken);

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

        public async Task<bool> UpdateProfileAsync(
            long userId,
            string? fullName,
            string? avatarUrl,
            CancellationToken cancellationToken = default)
        {
            if (userId <= 0)
            {
                return false;
            }

            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateCommandAsync(UserAccountUpdateProfileProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.Add(
                        new SqlParameter("@UserId", SqlDbType.BigInt) { Value = userId });

                    command.Parameters.Add(
                        new SqlParameter("@FullName", SqlDbType.NVarChar, 200)
                        {
                            Value = !string.IsNullOrWhiteSpace(fullName)
                                ? fullName.Trim()
                                : DBNull.Value
                        });

                    command.Parameters.Add(
                        new SqlParameter("@AvatarUrl", SqlDbType.NVarChar, 800)
                        {
                            Value = !string.IsNullOrWhiteSpace(avatarUrl)
                                ? avatarUrl.Trim()
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

        public async Task<bool> UpdatePasswordAsync(
            long userId,
            string passwordHash,
            CancellationToken cancellationToken = default)
        {
            if (userId <= 0 || string.IsNullOrWhiteSpace(passwordHash))
            {
                return false;
            }

            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateCommandAsync(UserAccountUpdatePasswordProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.Add(
                        new SqlParameter("@UserId", SqlDbType.BigInt) { Value = userId });

                    command.Parameters.Add(
                        new SqlParameter("@PasswordHash", SqlDbType.NVarChar, 500)
                        {
                            Value = passwordHash.Trim()
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

        public async Task<bool> UpdateLastLoginAsync(
            long userId,
            CancellationToken cancellationToken = default)
        {
            if (userId <= 0)
            {
                return false;
            }

            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateCommandAsync(UserAccountUpdateLastLoginProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.Add(
                        new SqlParameter("@UserId", SqlDbType.BigInt) { Value = userId });

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

        public async Task<bool> SetEmailVerifiedAsync(
            long userId,
            CancellationToken cancellationToken = default)
        {
            if (userId <= 0)
            {
                return false;
            }

            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateCommandAsync(UserAccountSetEmailVerifiedProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.Add(
                        new SqlParameter("@UserId", SqlDbType.BigInt)
                        {
                            Value = userId
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

        public async Task<bool> UpdateStatusAsync(
            long userId,
            UserAccountStatus status,
            DateTime? lockedUntil,
            CancellationToken cancellationToken = default)
        {
            if (userId <= 0)
            {
                return false;
            }

            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateCommandAsync(UserAccountUpdateStatusProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.Add(
                        new SqlParameter("@UserId", SqlDbType.BigInt) { Value = userId });

                    command.Parameters.Add(
                        new SqlParameter("@Status", SqlDbType.VarChar, 20)
                        {
                            Value = status.ToString()
                        });

                    command.Parameters.Add(
                        new SqlParameter("@LockedUntil", SqlDbType.DateTime2)
                        {
                            Value = lockedUntil.HasValue
                                ? lockedUntil.Value
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

        private static UserAccount MapUserAccount(SqlDataReader reader)
        {
            return UserAccount.Rehydrate(
                userId: reader.GetInt64(reader.GetOrdinal("UserId")),
                publicId: reader.GetString(reader.GetOrdinal("PublicId")),
                email: reader.GetString(reader.GetOrdinal("Email")),
                emailNormalized: reader.GetString(reader.GetOrdinal("EmailNormalized")),
                passwordHash: reader.GetString(reader.GetOrdinal("PasswordHash")),
                fullName: ReadNullableString(reader, "FullName"),
                avatarUrl: ReadNullableString(reader, "AvatarUrl"),
                isEmailVerified: reader.GetBoolean(reader.GetOrdinal("IsEmailVerified")),
                emailVerifiedAt: ReadNullableDateTime(reader, "EmailVerifiedAt"),
                status: ParseStatus(reader.GetString(reader.GetOrdinal("Status"))),
                lockedUntil: ReadNullableDateTime(reader, "LockedUntil"),
                createdAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                updatedAt: reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                lastLoginAt: ReadNullableDateTime(reader, "LastLoginAt"),
                version: reader.GetInt32(reader.GetOrdinal("Version")));
        }

        private static UserAccountStatus ParseStatus(string value)
        {
            return Enum.Parse<UserAccountStatus>(value, ignoreCase: true);
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