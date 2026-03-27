using System.Data;
using Content.Application.Ports.Persistence;
using Content.Infrastructure.Persistence.Sql;
using Microsoft.Data.SqlClient;

namespace Content.Infrastructure.Persistence.Repositories
{
    public sealed class ArticleLifecycleEventRepository : IArticleLifecycleEventRepository
    {
        private readonly ContentUnitOfWork _unitOfWork;

        public ArticleLifecycleEventRepository(ContentUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task InsertAsync(
            long articleId,
            string actionType,
            string? fromStatus,
            string? toStatus,
            string? reason,
            DateTime occurredAt,
            long? actorUserId,
            CancellationToken cancellationToken = default)
        {
            using SqlCommand command = CreateTransactionalCommand("Content_ArticleLifecycleEvent_Insert");

            command.Parameters.AddRange(
            [
                new SqlParameter("@ArticleId", SqlDbType.BigInt) { Value = articleId },
                new SqlParameter("@ActionType", SqlDbType.NVarChar, 30) { Value = actionType },
                new SqlParameter("@FromStatus", SqlDbType.NVarChar, 30) { Value = ToDbValue(fromStatus) },
                new SqlParameter("@ToStatus", SqlDbType.NVarChar, 30) { Value = ToDbValue(toStatus) },
                new SqlParameter("@Reason", SqlDbType.NVarChar, 1000) { Value = ToDbValue(reason) },
                new SqlParameter("@OccurredAt", SqlDbType.DateTime2) { Value = occurredAt },
                new SqlParameter("@ActorUserId", SqlDbType.BigInt) { Value = ToDbValue(actorUserId) }
            ]);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private SqlCommand CreateTransactionalCommand(string storedProcedureName)
        {
            SqlCommand command = _unitOfWork.Connection.CreateCommand();
            command.Transaction = _unitOfWork.Transaction;
            command.CommandText = storedProcedureName;
            command.CommandType = CommandType.StoredProcedure;

            return command;
        }

        private static object ToDbValue(object? value) => value ?? DBNull.Value;
    }
}

