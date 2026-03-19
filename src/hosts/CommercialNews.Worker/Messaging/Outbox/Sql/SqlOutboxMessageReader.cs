using System.Data;
using CommercialNews.Worker.Messaging.Outbox.Models;
using CommercialNews.Worker.Messaging.Outbox.Ports;
using Microsoft.Data.SqlClient;

namespace CommercialNews.Worker.Messaging.Outbox.Sql
{
    public sealed class SqlOutboxMessageReader : IOutboxMessageReader
    {
        private readonly WorkerSqlConnectionFactory _connectionFactory;

        public SqlOutboxMessageReader(WorkerSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IReadOnlyList<OutboxMessageRecord>> SelectPendingAsync(
            int topN,
            DateTime? nowUtc,
            CancellationToken cancellationToken)
        {
            var result = new List<OutboxMessageRecord>();

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand("[notifications].[OutboxMessage_SelectPending]", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@TopN", SqlDbType.Int)
            {
                Value = topN
            });

            command.Parameters.Add(new SqlParameter("@Now", SqlDbType.DateTime2)
            {
                Value = (object?)nowUtc ?? DBNull.Value
            });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(Map(reader));
            }

            return result;
        }

        private static OutboxMessageRecord Map(SqlDataReader reader)
        {
            return new OutboxMessageRecord
            {
                OutboxMessageId = reader.GetInt64(reader.GetOrdinal("OutboxMessageId")),
                MessageId = reader.GetString(reader.GetOrdinal("MessageId")),
                EventType = reader.GetString(reader.GetOrdinal("EventType")),
                AggregateType = reader.GetString(reader.GetOrdinal("AggregateType")),
                AggregateId = reader.GetString(reader.GetOrdinal("AggregateId")),
                AggregatePublicId = reader.IsDBNull(reader.GetOrdinal("AggregatePublicId")) ? null : reader.GetString(reader.GetOrdinal("AggregatePublicId")),
                AggregateVersion = reader.IsDBNull(reader.GetOrdinal("AggregateVersion")) ? null : reader.GetInt32(reader.GetOrdinal("AggregateVersion")),
                Payload = reader.GetString(reader.GetOrdinal("Payload")),
                Headers = reader.IsDBNull(reader.GetOrdinal("Headers")) ? null : reader.GetString(reader.GetOrdinal("Headers")),
                CorrelationId = reader.IsDBNull(reader.GetOrdinal("CorrelationId")) ? null : reader.GetString(reader.GetOrdinal("CorrelationId")),
                InitiatorUserId = reader.IsDBNull(reader.GetOrdinal("InitiatorUserId")) ? null : reader.GetInt64(reader.GetOrdinal("InitiatorUserId")),
                Status = reader.GetString(reader.GetOrdinal("Status")),
                AttemptCount = reader.GetInt32(reader.GetOrdinal("AttemptCount")),
                NextRetryAt = reader.IsDBNull(reader.GetOrdinal("NextRetryAt")) ? null : reader.GetDateTime(reader.GetOrdinal("NextRetryAt")),
                LastAttemptAt = reader.IsDBNull(reader.GetOrdinal("LastAttemptAt")) ? null : reader.GetDateTime(reader.GetOrdinal("LastAttemptAt")),
                PublishedAt = reader.IsDBNull(reader.GetOrdinal("PublishedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("PublishedAt")),
                LastError = reader.IsDBNull(reader.GetOrdinal("LastError")) ? null : reader.GetString(reader.GetOrdinal("LastError")),
                OccurredAt = reader.GetDateTime(reader.GetOrdinal("OccurredAt")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
            };
        }
    }
}