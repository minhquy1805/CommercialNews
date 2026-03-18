using Identity.Application.Contracts.Ports;
using Identity.Domain.Entities;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Identity.Infrastructure.Persistence.Sql.Repositories
{
    public sealed class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly IdentityUnitOfWork _unitOfWork;

        public RefreshTokenRepository(IdentityUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task InsertAsync(
            RefreshToken refreshToken,
            CancellationToken cancellationToken)
        {
            await using var command = new SqlCommand(
                "[identity].[RefreshToken_Insert]",
                _unitOfWork.Connection,
                _unitOfWork.Transaction)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.BigInt)
            {
                Value = refreshToken.UserId
            });

            command.Parameters.Add(new SqlParameter("@TokenHash", SqlDbType.VarBinary, 32)
            {
                Value = refreshToken.TokenHash
            });

            command.Parameters.Add(new SqlParameter("@ExpiresAt", SqlDbType.DateTime2)
            {
                Value = refreshToken.ExpiresAt
            });

            command.Parameters.Add(new SqlParameter("@CreatedIp", SqlDbType.NVarChar, 45)
            {
                Value = (object?)refreshToken.CreatedIp ?? DBNull.Value
            });

            command.Parameters.Add(new SqlParameter("@UserAgent", SqlDbType.NVarChar, 300)
            {
                Value = (object?)refreshToken.UserAgent ?? DBNull.Value
            });

            command.Parameters.Add(new SqlParameter("@CorrelationId", SqlDbType.NVarChar, 100)
            {
                Value = (object?)refreshToken.CorrelationId ?? DBNull.Value
            });

            var refreshTokenIdParameter = new SqlParameter("@RefreshTokenId", SqlDbType.BigInt)
            {
                Direction = ParameterDirection.Output
            };
            command.Parameters.Add(refreshTokenIdParameter);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
