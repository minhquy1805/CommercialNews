using System.Data;
using Identity.Application.Contracts.Ports;
using Microsoft.Data.SqlClient;

namespace Identity.Infrastructure.Persistence.Sql
{
    public sealed class RefreshTokenRevocationService : IRefreshTokenRevocationService
    {
        private readonly IdentityUnitOfWork _unitOfWork;

        public RefreshTokenRevocationService(IdentityUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task RevokeAllActiveByUserIdAsync(
            long userId,
            string? revokedReason,
            CancellationToken cancellationToken)
        {
            await using var command = new SqlCommand(
                "[identity].[RefreshToken_RevokeAllActiveByUserId]",
                _unitOfWork.Connection,
                _unitOfWork.Transaction)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.BigInt)
            {
                Value = userId
            });

            command.Parameters.Add(new SqlParameter("@RevokedReason", SqlDbType.NVarChar, 200)
            {
                Value = (object?)revokedReason ?? DBNull.Value
            });

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}

