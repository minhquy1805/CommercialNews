using System.Data;
using System.Text.Json;
using CommercialNews.BuildingBlocks.SharedKernel.Identifiers;
using Identity.Application.Ports.Services;
using Identity.Infrastructure.Persistence.Exceptions;
using Identity.Infrastructure.Persistence.Sql;
using Microsoft.Data.SqlClient;

namespace Identity.Infrastructure.Services;

public sealed class IdentityNotificationOutboxWriter : IIdentityNotificationOutboxWriter
{
    private const string OutboxMessageInsertProc =
        "[notifications].[OutboxMessage_Insert]";

    private const string DevVerifyEmailEndpoint =
        "http://localhost:5226/api/v1/identity/auth/verify-email";

    private readonly IdentityUnitOfWork _unitOfWork;
    private readonly IPublicIdGenerator _publicIdGenerator;
    private readonly IdentitySqlExceptionTranslator _sqlExceptionTranslator;

    public IdentityNotificationOutboxWriter(
        IdentityUnitOfWork unitOfWork,
        IPublicIdGenerator publicIdGenerator,
        IdentitySqlExceptionTranslator sqlExceptionTranslator)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _publicIdGenerator = publicIdGenerator ?? throw new ArgumentNullException(nameof(publicIdGenerator));
        _sqlExceptionTranslator = sqlExceptionTranslator ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
    }

    public async Task EnqueueVerificationEmailAsync(
        long userId,
        string userPublicId,
        string email,
        string? fullName,
        string rawVerificationToken,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(userPublicId))
        {
            throw new ArgumentException("User public id is required.", nameof(userPublicId));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(rawVerificationToken))
        {
            throw new ArgumentException("Raw verification token is required.", nameof(rawVerificationToken));
        }

        if (occurredAtUtc == default)
        {
            throw new ArgumentException("OccurredAtUtc is required.", nameof(occurredAtUtc));
        }

        string messageId = _publicIdGenerator.NewId();
        string verificationUrl = BuildVerificationUrl(rawVerificationToken);

        string payload = JsonSerializer.Serialize(new
        {
            businessDedupeKey = $"identity:verify-email:{userId}:{rawVerificationToken}",
            recipientUserId = userId,
            toEmail = email.Trim(),
            templateKey = "VerifyEmail",
            templateVersion = 1,
            subject = "Verify your email address",
            provider = "smtp",
            correlationId = userPublicId,
            variables = new
            {
                UserName = string.IsNullOrWhiteSpace(fullName) ? email.Trim() : fullName.Trim(),
                VerificationUrl = verificationUrl
            }
        });

        try
        {
            using SqlCommand command = CreateTransactionalCommand(OutboxMessageInsertProc);

            SqlParameter outboxMessageIdParameter = new("@OutboxMessageId", SqlDbType.BigInt)
            {
                Direction = ParameterDirection.Output
            };

            command.Parameters.AddRange(
            [
                new SqlParameter("@MessageId", SqlDbType.Char, 26) { Value = messageId },
                new SqlParameter("@EventType", SqlDbType.NVarChar, 200) { Value = "Identity.VerificationEmailRequested" },
                new SqlParameter("@AggregateType", SqlDbType.NVarChar, 100) { Value = "UserAccount" },
                new SqlParameter("@AggregateId", SqlDbType.NVarChar, 100) { Value = userId.ToString() },
                new SqlParameter("@AggregatePublicId", SqlDbType.Char, 26) { Value = userPublicId.Trim() },
                new SqlParameter("@AggregateVersion", SqlDbType.Int) { Value = DBNull.Value },
                new SqlParameter("@Payload", SqlDbType.NVarChar, -1) { Value = payload },
                new SqlParameter("@Headers", SqlDbType.NVarChar, -1) { Value = DBNull.Value },
                new SqlParameter("@CorrelationId", SqlDbType.NVarChar, 100) { Value = userPublicId.Trim() },
                new SqlParameter("@InitiatorUserId", SqlDbType.BigInt) { Value = userId },
                new SqlParameter("@Priority", SqlDbType.TinyInt) { Value = 5 },
                new SqlParameter("@OccurredAt", SqlDbType.DateTime2) { Value = occurredAtUtc },
                outboxMessageIdParameter
            ]);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task EnqueuePasswordChangedEmailAsync(
        long userId,
        string userPublicId,
        string email,
        string? fullName,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(userPublicId))
        {
            throw new ArgumentException("User public id is required.", nameof(userPublicId));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        if (occurredAtUtc == default)
        {
            throw new ArgumentException("OccurredAtUtc is required.", nameof(occurredAtUtc));
        }

        string messageId = _publicIdGenerator.NewId();

        string payload = JsonSerializer.Serialize(new
        {
            businessDedupeKey = $"identity:password-changed:{userId}:{occurredAtUtc:O}",
            recipientUserId = userId,
            toEmail = email.Trim(),
            templateKey = "PasswordChanged",
            templateVersion = 1,
            subject = "Your password was changed",
            provider = "smtp",
            correlationId = userPublicId,
            variables = new
            {
                UserName = string.IsNullOrWhiteSpace(fullName) ? email.Trim() : fullName.Trim(),
                ChangedAtUtc = occurredAtUtc.ToString("O")
            }
        });

        try
        {
            using SqlCommand command = CreateTransactionalCommand(OutboxMessageInsertProc);

            SqlParameter outboxMessageIdParameter = new("@OutboxMessageId", SqlDbType.BigInt)
            {
                Direction = ParameterDirection.Output
            };

            command.Parameters.AddRange(
            [
                new SqlParameter("@MessageId", SqlDbType.Char, 26) { Value = messageId },
                new SqlParameter("@EventType", SqlDbType.NVarChar, 200) { Value = "Identity.PasswordChangedEmailRequested" },
                new SqlParameter("@AggregateType", SqlDbType.NVarChar, 100) { Value = "UserAccount" },
                new SqlParameter("@AggregateId", SqlDbType.NVarChar, 100) { Value = userId.ToString() },
                new SqlParameter("@AggregatePublicId", SqlDbType.Char, 26) { Value = userPublicId.Trim() },
                new SqlParameter("@AggregateVersion", SqlDbType.Int) { Value = DBNull.Value },
                new SqlParameter("@Payload", SqlDbType.NVarChar, -1) { Value = payload },
                new SqlParameter("@Headers", SqlDbType.NVarChar, -1) { Value = DBNull.Value },
                new SqlParameter("@CorrelationId", SqlDbType.NVarChar, 100) { Value = userPublicId.Trim() },
                new SqlParameter("@InitiatorUserId", SqlDbType.BigInt) { Value = userId },
                new SqlParameter("@Priority", SqlDbType.TinyInt) { Value = 5 },
                new SqlParameter("@OccurredAt", SqlDbType.DateTime2) { Value = occurredAtUtc },
                outboxMessageIdParameter
            ]);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task EnqueuePasswordResetEmailAsync(
        long userId,
        string userPublicId,
        string email,
        string? fullName,
        string rawResetToken,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(userPublicId))
        {
            throw new ArgumentException("User public id is required.", nameof(userPublicId));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(rawResetToken))
        {
            throw new ArgumentException("Raw reset token is required.", nameof(rawResetToken));
        }

        if (occurredAtUtc == default)
        {
            throw new ArgumentException("OccurredAtUtc is required.", nameof(occurredAtUtc));
        }

        string messageId = _publicIdGenerator.NewId();
        string resetUrl = BuildResetUrl(rawResetToken);

        string payload = JsonSerializer.Serialize(new
        {
            businessDedupeKey = $"identity:password-reset:{userId}:{rawResetToken}",
            recipientUserId = userId,
            toEmail = email.Trim(),
            templateKey = "ResetPassword",
            templateVersion = 1,
            subject = "Reset your password",
            provider = "smtp",
            correlationId = userPublicId,
            variables = new
            {
                UserName = string.IsNullOrWhiteSpace(fullName) ? email.Trim() : fullName.Trim(),
                ResetUrl = resetUrl
            }
        });

        try
        {
            using SqlCommand command = CreateTransactionalCommand(OutboxMessageInsertProc);

            SqlParameter outboxMessageIdParameter = new("@OutboxMessageId", SqlDbType.BigInt)
            {
                Direction = ParameterDirection.Output
            };

            command.Parameters.AddRange(
            [
                new SqlParameter("@MessageId", SqlDbType.Char, 26) { Value = messageId },
                new SqlParameter("@EventType", SqlDbType.NVarChar, 200) { Value = "Identity.PasswordResetRequested" },
                new SqlParameter("@AggregateType", SqlDbType.NVarChar, 100) { Value = "UserAccount" },
                new SqlParameter("@AggregateId", SqlDbType.NVarChar, 100) { Value = userId.ToString() },
                new SqlParameter("@AggregatePublicId", SqlDbType.Char, 26) { Value = userPublicId.Trim() },
                new SqlParameter("@AggregateVersion", SqlDbType.Int) { Value = DBNull.Value },
                new SqlParameter("@Payload", SqlDbType.NVarChar, -1) { Value = payload },
                new SqlParameter("@Headers", SqlDbType.NVarChar, -1) { Value = DBNull.Value },
                new SqlParameter("@CorrelationId", SqlDbType.NVarChar, 100) { Value = userPublicId.Trim() },
                new SqlParameter("@InitiatorUserId", SqlDbType.BigInt) { Value = userId },
                new SqlParameter("@Priority", SqlDbType.TinyInt) { Value = 5 },
                new SqlParameter("@OccurredAt", SqlDbType.DateTime2) { Value = occurredAtUtc },
                outboxMessageIdParameter
            ]);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    private static string BuildResetUrl(string rawResetToken)
    {
        return $"http://localhost:5226/api/v1/identity/auth/reset-password?token={Uri.EscapeDataString(rawResetToken)}";
    }

    private SqlCommand CreateTransactionalCommand(string storedProcedureName)
    {
        SqlCommand command = _unitOfWork.Connection.CreateCommand();
        command.Transaction = _unitOfWork.Transaction;
        command.CommandText = storedProcedureName;
        command.CommandType = CommandType.StoredProcedure;
        return command;
    }

    private static string BuildVerificationUrl(string rawVerificationToken)
    {
        return $"{DevVerifyEmailEndpoint}?token={Uri.EscapeDataString(rawVerificationToken)}";
    }
}