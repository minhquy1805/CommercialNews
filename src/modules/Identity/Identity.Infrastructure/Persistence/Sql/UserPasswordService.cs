using System.Data;
using Identity.Application.Contracts.Ports;
using Microsoft.Data.SqlClient;

namespace Identity.Infrastructure.Persistence.Sql
{
    public sealed class UserPasswordService : IUserPasswordService
    {
        private readonly IdentityUnitOfWork _unitOfWork;

        public UserPasswordService(IdentityUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task UpdatePasswordAsync(
            long userId,
            string newPasswordHash,
            CancellationToken cancellationToken)
        {
            await using var command = new SqlCommand(
                "[identity].[UserAccount_UpdatePassword]",
                _unitOfWork.Connection,
                _unitOfWork.Transaction)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.BigInt)
            {
                Value = userId
            });

            command.Parameters.Add(new SqlParameter("@PasswordHash", SqlDbType.NVarChar, 500)
            {
                Value = newPasswordHash
            });

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}