using Identity.Application.Contracts.Ports;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Identity.Infrastructure.Persistence.Sql.Repositories
{
    public sealed class UserAccountRepository : IUserAccountRepository
    {
        private readonly IdentitySqlConnectionFactory _connectionFactory;
        private readonly IdentityUnitOfWork _unitOfWork;

        public UserAccountRepository(
            IdentitySqlConnectionFactory connectionFactory,
            IdentityUnitOfWork unitOfWork)
        {
            _connectionFactory = connectionFactory;
            _unitOfWork = unitOfWork;
        }

        public async Task<UserAccount?> GetByEmailNormalizedAsync(
            string emailNormalized,
            CancellationToken cancellationToken)
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand("[identity].[UserAccount_SelectByEmailNormalized]", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@EmailNormalized", SqlDbType.NVarChar, 320)
            {
                Value = emailNormalized
            });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return MapUserAccount(reader);
        }

        public async Task<long> InsertAsync(
            UserAccount userAccount,
            CancellationToken cancellationToken)
        {
            await using var command = new SqlCommand(
                "[identity].[UserAccount_Insert]",
                _unitOfWork.Connection,
                _unitOfWork.Transaction)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@PublicId", SqlDbType.Char, 26)
            {
                Value = userAccount.PublicId
            });

            command.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar, 320)
            {
                Value = userAccount.Email
            });

            command.Parameters.Add(new SqlParameter("@EmailNormalized", SqlDbType.NVarChar, 320)
            {
                Value = userAccount.EmailNormalized
            });

            command.Parameters.Add(new SqlParameter("@PasswordHash", SqlDbType.NVarChar, 500)
            {
                Value = userAccount.PasswordHash
            });

            command.Parameters.Add(new SqlParameter("@FullName", SqlDbType.NVarChar, 200)
            {
                Value = (object?)userAccount.FullName ?? DBNull.Value
            });

            command.Parameters.Add(new SqlParameter("@AvatarUrl", SqlDbType.NVarChar, 800)
            {
                Value = (object?)userAccount.AvatarUrl ?? DBNull.Value
            });

            command.Parameters.Add(new SqlParameter("@Status", SqlDbType.VarChar, 20)
            {
                Value = userAccount.Status.ToString()
            });

            var userIdParameter = new SqlParameter("@UserId", SqlDbType.BigInt)
            {
                Direction = ParameterDirection.Output
            };
            command.Parameters.Add(userIdParameter);

            await command.ExecuteNonQueryAsync(cancellationToken);

            return Convert.ToInt64(userIdParameter.Value);
        }

        public async Task UpdateLastLoginAsync(
            long userId,
            CancellationToken cancellationToken)
        {
            await using var command = new SqlCommand(
                "[identity].[UserAccount_UpdateLastLogin]",
                _unitOfWork.Connection,
                _unitOfWork.Transaction)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.BigInt)
            {
                Value = userId
            });

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<UserAccount?> GetByIdAsync(
            long userId,
            CancellationToken cancellationToken)
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand("[identity].[UserAccount_SelectById]", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.BigInt)
            {
                Value = userId
            });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return MapUserAccount(reader);
        }

        private static UserAccount MapUserAccount(SqlDataReader reader)
        {
            return new UserAccount(
                userId: reader.GetInt64(reader.GetOrdinal("UserId")),
                publicId: reader.GetString(reader.GetOrdinal("PublicId")),
                email: reader.GetString(reader.GetOrdinal("Email")),
                emailNormalized: reader.GetString(reader.GetOrdinal("EmailNormalized")),
                passwordHash: reader.GetString(reader.GetOrdinal("PasswordHash")),
                fullName: reader.IsDBNull(reader.GetOrdinal("FullName"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("FullName")),
                avatarUrl: reader.IsDBNull(reader.GetOrdinal("AvatarUrl"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("AvatarUrl")),
                isEmailVerified: reader.GetBoolean(reader.GetOrdinal("IsEmailVerified")),
                emailVerifiedAt: reader.IsDBNull(reader.GetOrdinal("EmailVerifiedAt"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("EmailVerifiedAt")),
                status: Enum.Parse<UserAccountStatus>(
                    reader.GetString(reader.GetOrdinal("Status")),
                    ignoreCase: true),
                lockedUntil: reader.IsDBNull(reader.GetOrdinal("LockedUntil"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("LockedUntil")),
                createdAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                updatedAt: reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                lastLoginAt: reader.IsDBNull(reader.GetOrdinal("LastLoginAt"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("LastLoginAt")),
                version: reader.GetInt32(reader.GetOrdinal("Version")));
        }
    }
}
