using Identity.Application.Contracts.Ports;
using Identity.Domain.Entities;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Identity.Infrastructure.Persistence.Sql.Repositories
{
    public sealed class PasswordResetTokenRepository : IPasswordResetTokenRepository
    {
        private readonly IdentityUnitOfWork _unitOfWork;

        public PasswordResetTokenRepository(IdentityUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task RevokeActiveByUserIdAsync(
            long userId,
            CancellationToken cancellationToken)
        {
            await using var command = new SqlCommand(
                "[identity].[PasswordResetToken_RevokeActiveByUserId]",
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

        public async Task InsertAsync(
            PasswordResetToken token,
            CancellationToken cancellationToken)
        {
            await using var command = new SqlCommand(
                "[identity].[PasswordResetToken_Insert]",
                _unitOfWork.Connection,
                _unitOfWork.Transaction)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.BigInt)
            {
                Value = token.UserId
            });

            command.Parameters.Add(new SqlParameter("@TokenHash", SqlDbType.VarBinary, 32)
            {
                Value = token.TokenHash
            });

            command.Parameters.Add(new SqlParameter("@ExpiresAt", SqlDbType.DateTime2)
            {
                Value = token.ExpiresAt
            });

            command.Parameters.Add(new SqlParameter("@CreatedIp", SqlDbType.NVarChar, 45)
            {
                Value = (object?)token.CreatedIp ?? DBNull.Value
            });

            command.Parameters.Add(new SqlParameter("@CorrelationId", SqlDbType.NVarChar, 100)
            {
                Value = (object?)token.CorrelationId ?? DBNull.Value
            });

            var resetTokenIdParameter = new SqlParameter("@ResetTokenId", SqlDbType.BigInt)
            {
                Direction = ParameterDirection.Output
            };
            command.Parameters.Add(resetTokenIdParameter);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
