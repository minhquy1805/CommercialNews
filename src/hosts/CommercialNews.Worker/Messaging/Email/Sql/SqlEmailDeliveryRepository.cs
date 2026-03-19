using System.Data;
using CommercialNews.Worker.Messaging.Email.Ports;
using CommercialNews.Worker.Messaging.Outbox.Sql;
using Microsoft.Data.SqlClient;

namespace CommercialNews.Worker.Messaging.Email.Sql
{
    public sealed class SqlEmailDeliveryRepository : IEmailDeliveryRepository
    {
        private readonly WorkerSqlConnectionFactory _connectionFactory;

        public SqlEmailDeliveryRepository(WorkerSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<long> InsertAsync(
            string messageId,
            long? userId,
            string toEmail,
            string templateKey,
            string? subject,
            string? correlationId,
            CancellationToken cancellationToken)
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(
                "[notifications].[EmailDelivery_Insert]",
                connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@MessageId", SqlDbType.Char, 26)
            {
                Value = messageId
            });

            command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.BigInt)
            {
                Value = (object?)userId ?? DBNull.Value
            });

            command.Parameters.Add(new SqlParameter("@ToEmail", SqlDbType.NVarChar, 320)
            {
                Value = toEmail
            });

            command.Parameters.Add(new SqlParameter("@TemplateKey", SqlDbType.NVarChar, 100)
            {
                Value = templateKey
            });

            command.Parameters.Add(new SqlParameter("@Subject", SqlDbType.NVarChar, 300)
            {
                Value = (object?)subject ?? DBNull.Value
            });

            command.Parameters.Add(new SqlParameter("@CorrelationId", SqlDbType.NVarChar, 100)
            {
                Value = (object?)correlationId ?? DBNull.Value
            });

            var emailDeliveryIdParameter = new SqlParameter("@EmailDeliveryId", SqlDbType.BigInt)
            {
                Direction = ParameterDirection.Output
            };
            command.Parameters.Add(emailDeliveryIdParameter);

            await command.ExecuteNonQueryAsync(cancellationToken);

            return Convert.ToInt64(emailDeliveryIdParameter.Value);
        }

        public async Task MarkSentAsync(
            long emailDeliveryId,
            string? providerMessageId,
            CancellationToken cancellationToken)
        {
            await ExecuteNonQueryAsync(
                "[notifications].[EmailDelivery_MarkSent]",
                command =>
                {
                    command.Parameters.Add(new SqlParameter("@EmailDeliveryId", SqlDbType.BigInt)
                    {
                        Value = emailDeliveryId
                    });

                    command.Parameters.Add(new SqlParameter("@ProviderMessageId", SqlDbType.NVarChar, 200)
                    {
                        Value = (object?)providerMessageId ?? DBNull.Value
                    });
                },
                cancellationToken);
        }

        public async Task MarkFailedAsync(
            long emailDeliveryId,
            DateTime? nextRetryAt,
            string? lastError,
            CancellationToken cancellationToken)
        {
            await ExecuteNonQueryAsync(
                "[notifications].[EmailDelivery_MarkFailed]",
                command =>
                {
                    command.Parameters.Add(new SqlParameter("@EmailDeliveryId", SqlDbType.BigInt)
                    {
                        Value = emailDeliveryId
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
            long emailDeliveryId,
            string? lastError,
            CancellationToken cancellationToken)
        {
            await ExecuteNonQueryAsync(
                "[notifications].[EmailDelivery_MarkDeadLetter]",
                command =>
                {
                    command.Parameters.Add(new SqlParameter("@EmailDeliveryId", SqlDbType.BigInt)
                    {
                        Value = emailDeliveryId
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