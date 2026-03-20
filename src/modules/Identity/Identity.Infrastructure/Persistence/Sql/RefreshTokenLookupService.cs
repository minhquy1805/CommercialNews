using System.Data;
using Identity.Application.Contracts.Ports;
using Identity.Domain.Entities;
using Microsoft.Data.SqlClient;

namespace Identity.Infrastructure.Persistence.Sql
{
    public sealed class RefreshTokenLookupService : IRefreshTokenLookupService
    {
        private readonly IdentitySqlConnectionFactory _connectionFactory;

        public RefreshTokenLookupService(IdentitySqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<RefreshToken?> GetByTokenHashAsync(
            byte[] tokenHash,
            CancellationToken cancellationToken)
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(
                "[identity].[RefreshToken_SelectByTokenHash]",
                connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@TokenHash", SqlDbType.VarBinary, 32)
            {
                Value = tokenHash
            });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return new RefreshToken(
                refreshTokenId: reader.GetInt64(reader.GetOrdinal("RefreshTokenId")),
                userId: reader.GetInt64(reader.GetOrdinal("UserId")),
                tokenHash: (byte[])reader["TokenHash"],
                createdAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                expiresAt: reader.GetDateTime(reader.GetOrdinal("ExpiresAt")),
                revokedAt: reader.IsDBNull(reader.GetOrdinal("RevokedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("RevokedAt")),
                revokedReason: reader.IsDBNull(reader.GetOrdinal("RevokedReason")) ? null : reader.GetString(reader.GetOrdinal("RevokedReason")),
                replacedByTokenHash: reader.IsDBNull(reader.GetOrdinal("ReplacedByTokenHash")) ? null : (byte[])reader["ReplacedByTokenHash"],
                createdIp: reader.IsDBNull(reader.GetOrdinal("CreatedIp")) ? null : reader.GetString(reader.GetOrdinal("CreatedIp")),
                userAgent: reader.IsDBNull(reader.GetOrdinal("UserAgent")) ? null : reader.GetString(reader.GetOrdinal("UserAgent")),
                correlationId: reader.IsDBNull(reader.GetOrdinal("CorrelationId")) ? null : reader.GetString(reader.GetOrdinal("CorrelationId")));
        }
    }   
}