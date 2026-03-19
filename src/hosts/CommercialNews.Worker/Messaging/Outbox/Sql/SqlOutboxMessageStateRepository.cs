using System.Data;
using CommercialNews.Worker.Messaging.Outbox.Ports;
using Microsoft.Data.SqlClient;

namespace CommercialNews.Worker.Messaging.Outbox.Sql
{
    public sealed class SqlOutboxMessageStateRepository : IOutboxMessageStateRepository
    {
        private readonly WorkerSqlConnectionFactory _connectionFactory;

        public SqlOutboxMessageStateRepository(WorkerSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task MarkProcessingAsync(long outboxMessageId, CancellationToken cancellationToken)
        {
            await ExecuteNonQueryAsync(
                "[notifications].[OutboxMessage_MarkProcessing]",
                command =>
                {
                    command.Parameters.Add(new SqlParameter("@OutboxMessageId", SqlDbType.BigInt)
                    {
                        Value = outboxMessageId
                    });
                },
                cancellationToken);
        }

        public async Task MarkPublishedAsync(long outboxMessageId, CancellationToken cancellationToken)
        {
            await ExecuteNonQueryAsync(
                "[notifications].[OutboxMessage_MarkPublished]",
                command =>
                {
                    command.Parameters.Add(new SqlParameter("@OutboxMessageId", SqlDbType.BigInt)
                    {
                        Value = outboxMessageId
                    });
                },
                cancellationToken);
        }

        public async Task MarkFailedAsync(
            long outboxMessageId,
            DateTime? nextRetryAt,
            string? lastError,
            CancellationToken cancellationToken)
        {
            await ExecuteNonQueryAsync(
                "[notifications].[OutboxMessage_MarkFailed]",
                command =>
                {
                    command.Parameters.Add(new SqlParameter("@OutboxMessageId", SqlDbType.BigInt)
                    {
                        Value = outboxMessageId
                    });

                    command.Parameters.Add(new SqlParameter("@NextRetryAt", SqlDbType.DateTime2)
                    {
                        Value = (object?)nextRetryAt ?? DBNull.Value
                    });

                    command.Parameters.Add(new SqlParameter("@LastError", SqlDbType.NVarChar, 2000)
                    {
                        Value = (object?)lastError ?? DBNull.Value
                    });
                },
                cancellationToken);
        }

        public async Task MarkDeadLetterAsync(
            long outboxMessageId,
            string? lastError,
            CancellationToken cancellationToken)
        {
            await ExecuteNonQueryAsync(
                "[notifications].[OutboxMessage_MarkDeadLetter]",
                command =>
                {
                    command.Parameters.Add(new SqlParameter("@OutboxMessageId", SqlDbType.BigInt)
                    {
                        Value = outboxMessageId
                    });

                    command.Parameters.Add(new SqlParameter("@LastError", SqlDbType.NVarChar, 2000)
                    {
                        Value = (object?)lastError ?? DBNull.Value
                    });
                },
                cancellationToken);
        }

        private async Task ExecuteNonQueryAsync(
            string storedProcedure,
            Action<SqlCommand> configure,
            CancellationToken cancellationToken)
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(storedProcedure, connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            configure(command);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}