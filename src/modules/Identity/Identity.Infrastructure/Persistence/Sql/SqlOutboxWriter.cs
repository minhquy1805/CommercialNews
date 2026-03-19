using System.Data;
using CommercialNews.BuildingBlocks.Messaging.Outbox;
using Microsoft.Data.SqlClient;

namespace Identity.Infrastructure.Persistence.Sql
{
    public sealed class SqlOutboxWriter : IOutboxWriter
    {
        private readonly IdentityUnitOfWork _unitOfWork;
        public SqlOutboxWriter(IdentityUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task WriteAsync(
            string messageId,
            string eventType,
            string aggregateType,
            string aggregateId,
            string? aggregatePublicId,
            int? aggregateVersion,
            string payload,
            string? headers,
            string? correlationId,
            long? initiatorUserId,
            DateTime occurredAtUtc,
            CancellationToken cancellationToken)
        {
            await using var command = new SqlCommand(
                "[notifications].[OutboxMessage_Insert]",
                _unitOfWork.Connection,
                _unitOfWork.Transaction)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@MessageId", SqlDbType.Char, 26)
            {
                Value = messageId
            });

            command.Parameters.Add(new SqlParameter("@EventType", SqlDbType.NVarChar, 200)
            {
                Value = eventType
            });

            command.Parameters.Add(new SqlParameter("@AggregateType", SqlDbType.NVarChar, 100)
            {
                Value = aggregateType
            });

            command.Parameters.Add(new SqlParameter("@AggregateId", SqlDbType.NVarChar, 100)
            {
                Value = aggregateId
            });

            command.Parameters.Add(new SqlParameter("@AggregatePublicId", SqlDbType.Char, 26)
            {
                Value = (object?)aggregatePublicId ?? DBNull.Value
            });

            command.Parameters.Add(new SqlParameter("@AggregateVersion", SqlDbType.Int)
            {
                Value = (object?)aggregateVersion ?? DBNull.Value
            });

            command.Parameters.Add(new SqlParameter("@Payload", SqlDbType.NVarChar, -1)
            {
                Value = payload
            });

            command.Parameters.Add(new SqlParameter("@Headers", SqlDbType.NVarChar, -1)
            {
                Value = (object?)headers ?? DBNull.Value
            });

            command.Parameters.Add(new SqlParameter("@CorrelationId", SqlDbType.NVarChar, 100)
            {
                Value = (object?)correlationId ?? DBNull.Value
            });

            command.Parameters.Add(new SqlParameter("@InitiatorUserId", SqlDbType.BigInt)
            {
                Value = (object?)initiatorUserId ?? DBNull.Value
            });

            command.Parameters.Add(new SqlParameter("@OccurredAt", SqlDbType.DateTime2)
            {
                Value = occurredAtUtc
            });

            var outboxMessageIdParameter = new SqlParameter("@OutboxMessageId", SqlDbType.BigInt)
            {
                Direction = ParameterDirection.Output
            };
            command.Parameters.Add(outboxMessageIdParameter);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}