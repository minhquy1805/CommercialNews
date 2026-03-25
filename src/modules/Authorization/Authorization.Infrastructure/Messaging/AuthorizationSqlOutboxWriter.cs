using System.Data;
using Authorization.Infrastructure.Persistence.Sql;
using CommercialNews.BuildingBlocks.Messaging.Outbox;
using Microsoft.Data.SqlClient;

namespace Authorization.Infrastructure.Messaging.Outbox
{
    public sealed class AuthorizationSqlOutboxWriter : IOutboxWriter
    {
        private readonly AuthorizationUnitOfWork _unitOfWork;

        public AuthorizationSqlOutboxWriter(AuthorizationUnitOfWork unitOfWork)
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
            if (!_unitOfWork.HasActiveConnection || !_unitOfWork.HasActiveTransaction)
            {
                throw new InvalidOperationException(
                    "Authorization outbox write requires an active transaction.");
            }

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

            command.Parameters.Add(new SqlParameter("@Payload", SqlDbType.NVarChar)
            {
                Value = payload
            });

            command.Parameters.Add(new SqlParameter("@Headers", SqlDbType.NVarChar)
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