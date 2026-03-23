using System.Data;
using Identity.Application.Contracts.Ports;
using Microsoft.Data.SqlClient;

namespace Identity.Infrastructure.Persistence.Sql
{
    public sealed class UserProfileService : IUserProfileService
    {
        private readonly IdentityUnitOfWork _unitOfWork;

        public UserProfileService(IdentityUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task UpdateProfileAsync(
            long userId,
            string? fullName,
            string? avatarUrl,
            CancellationToken cancellationToken)
        {
            await using var command = new SqlCommand(
                "[identity].[UserAccount_UpdateProfile]",
                _unitOfWork.Connection,
                _unitOfWork.Transaction)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.BigInt)
            {
                Value = userId
            });

            command.Parameters.Add(new SqlParameter("@FullName", SqlDbType.NVarChar, 200)
            {
                Value = (object?)fullName ?? DBNull.Value
            });

            command.Parameters.Add(new SqlParameter("@AvatarUrl", SqlDbType.NVarChar, 800)
            {
                Value = (object?)avatarUrl ?? DBNull.Value
            });

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}