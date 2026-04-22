using System.Data;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using Microsoft.Data.SqlClient;
using Notifications.Application.Models.QueryModels;
using Notifications.Application.Ports.Persistence;
using Notifications.Application.Ports.Transactions;
using Notifications.Infrastructure.Persistence.Exceptions;

namespace Notifications.Infrastructure.Persistence.Repositories;

public sealed class EmailDeliveryQueryRepository : IEmailDeliveryQueryRepository
{
    private const string EmailDeliverySelectSkipAndTakeProc =
        "[notifications].[EmailDelivery_Search]";

    private const string EmailDeliverySelectByIdProc =
        "[notifications].[EmailDelivery_SelectById]";

    private const string EmailDeliverySelectByMessageIdProc =
        "[notifications].[EmailDelivery_SelectByMessageId]";

    private const string EmailDeliveryAttemptSelectByEmailDeliveryIdProc =
        "[notifications].[EmailDeliveryAttempt_SelectByEmailDeliveryId]";

    private readonly INotificationsUnitOfWork _unitOfWork;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly NotificationsSqlExceptionTranslator _sqlExceptionTranslator;

    public EmailDeliveryQueryRepository(
        INotificationsUnitOfWork unitOfWork,
        ISqlConnectionFactory sqlConnectionFactory,
        NotificationsSqlExceptionTranslator sqlExceptionTranslator)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
        _sqlExceptionTranslator = sqlExceptionTranslator ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
    }

    public async Task<EmailDeliveryDetailResult?> GetByIdAsync(
        long emailDeliveryId,
        CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(EmailDeliverySelectByIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@EmailDeliveryId", SqlDbType.BigInt) { Value = emailDeliveryId });

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                EmailDeliveryDetailResult detail = MapEmailDeliveryDetail(reader);

                IReadOnlyList<EmailDeliveryAttemptResultItem> attempts =
                    await GetAttemptsInternalAsync(detail.EmailDeliveryId, cancellationToken);

                return new EmailDeliveryDetailResult
                {
                    EmailDeliveryId = detail.EmailDeliveryId,
                    MessageId = detail.MessageId,
                    BusinessDedupeKey = detail.BusinessDedupeKey,
                    RecipientUserId = detail.RecipientUserId,
                    ToEmail = detail.ToEmail,
                    TemplateKey = detail.TemplateKey,
                    Provider = detail.Provider,
                    Status = detail.Status,
                    AttemptCount = detail.AttemptCount,
                    LastAttemptAt = detail.LastAttemptAt,
                    NextRetryAt = detail.NextRetryAt,
                    SentAt = detail.SentAt,
                    LastErrorCode = detail.LastErrorCode,
                    LastErrorClass = detail.LastErrorClass,
                    CorrelationId = detail.CorrelationId,
                    CreatedAt = detail.CreatedAt,
                    UpdatedAt = detail.UpdatedAt,
                    Attempts = attempts
                };
            }
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
        finally
        {
            if (ownedConnection is not null)
            {
                await ownedConnection.DisposeAsync();
            }
        }
    }

    public async Task<EmailDeliveryDetailResult?> GetByMessageIdAsync(
        string messageId,
        CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(EmailDeliverySelectByMessageIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@MessageId", SqlDbType.Char, 26) { Value = messageId });

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                EmailDeliveryDetailResult detail = MapEmailDeliveryDetail(reader);

                IReadOnlyList<EmailDeliveryAttemptResultItem> attempts =
                    await GetAttemptsInternalAsync(detail.EmailDeliveryId, cancellationToken);

                return new EmailDeliveryDetailResult
                {
                    EmailDeliveryId = detail.EmailDeliveryId,
                    MessageId = detail.MessageId,
                    BusinessDedupeKey = detail.BusinessDedupeKey,
                    RecipientUserId = detail.RecipientUserId,
                    ToEmail = detail.ToEmail,
                    TemplateKey = detail.TemplateKey,
                    Provider = detail.Provider,
                    Status = detail.Status,
                    AttemptCount = detail.AttemptCount,
                    LastAttemptAt = detail.LastAttemptAt,
                    NextRetryAt = detail.NextRetryAt,
                    SentAt = detail.SentAt,
                    LastErrorCode = detail.LastErrorCode,
                    LastErrorClass = detail.LastErrorClass,
                    CorrelationId = detail.CorrelationId,
                    CreatedAt = detail.CreatedAt,
                    UpdatedAt = detail.UpdatedAt,
                    Attempts = attempts
                };
            }
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
        finally
        {
            if (ownedConnection is not null)
            {
                await ownedConnection.DisposeAsync();
            }
        }
    }

    public async Task<PagedQueryResult<EmailDeliveryListResultItem>> SelectSkipAndTakeAsync(
        EmailDeliveryListQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(EmailDeliverySelectSkipAndTakeProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                int page = query.Page <= 0 ? 1 : query.Page;
                int pageSize = query.PageSize <= 0 ? 20 : query.PageSize;

                command.Parameters.AddRange(
                [
                    new SqlParameter("@Page", SqlDbType.Int) { Value = page },
                    new SqlParameter("@PageSize", SqlDbType.Int) { Value = pageSize },
                    new SqlParameter("@FromCreatedAt", SqlDbType.DateTime2) { Value = ToDbValue(query.FromCreatedAt) },
                    new SqlParameter("@ToCreatedAt", SqlDbType.DateTime2) { Value = ToDbValue(query.ToCreatedAt) },
                    new SqlParameter("@RecipientUserId", SqlDbType.BigInt) { Value = ToDbValue(query.RecipientUserId) },
                    new SqlParameter("@TemplateKey", SqlDbType.NVarChar, 100) { Value = ToDbValue(query.TemplateKey) },
                    new SqlParameter("@Status", SqlDbType.VarChar, 20) { Value = ToDbValue(query.Status) },
                    new SqlParameter("@CorrelationId", SqlDbType.NVarChar, 100) { Value = ToDbValue(query.CorrelationId) },
                    new SqlParameter("@MessageId", SqlDbType.Char, 26) { Value = ToDbValue(query.MessageId) }
                ]);

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                List<EmailDeliveryListResultItem> items = [];
                int totalItems = 0;

                while (await reader.ReadAsync(cancellationToken))
                {
                    if (totalItems == 0)
                    {
                        totalItems = reader.GetInt32(reader.GetOrdinal("TotalCount"));
                    }

                    items.Add(MapEmailDeliveryListItem(reader));
                }

                return new PagedQueryResult<EmailDeliveryListResultItem>
                {
                    Items = items,
                    Page = page,
                    PageSize = pageSize,
                    TotalItems = totalItems
                };
            }
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
        finally
        {
            if (ownedConnection is not null)
            {
                await ownedConnection.DisposeAsync();
            }
        }
    }

    public async Task<IReadOnlyList<EmailDeliveryAttemptResultItem>> GetAttemptsByEmailDeliveryIdAsync(
        long emailDeliveryId,
        CancellationToken cancellationToken = default)
    {
        return await GetAttemptsInternalAsync(emailDeliveryId, cancellationToken);
    }

    private async Task<IReadOnlyList<EmailDeliveryAttemptResultItem>> GetAttemptsInternalAsync(
        long emailDeliveryId,
        CancellationToken cancellationToken)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(EmailDeliveryAttemptSelectByEmailDeliveryIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@EmailDeliveryId", SqlDbType.BigInt) { Value = emailDeliveryId });

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                List<EmailDeliveryAttemptResultItem> items = [];

                while (await reader.ReadAsync(cancellationToken))
                {
                    items.Add(MapEmailDeliveryAttempt(reader));
                }

                return items;
            }
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
        finally
        {
            if (ownedConnection is not null)
            {
                await ownedConnection.DisposeAsync();
            }
        }
    }

    private async Task<(SqlCommand Command, SqlConnection? OwnedConnection)> CreateReadCommandAsync(
        string storedProcedureName,
        CancellationToken cancellationToken)
    {
        if (_unitOfWork.HasActiveConnection)
        {
            SqlCommand ambientCommand = _unitOfWork.Connection.CreateCommand();
            ambientCommand.Transaction = _unitOfWork.HasActiveTransaction
                ? _unitOfWork.Transaction
                : null;
            ambientCommand.CommandText = storedProcedureName;
            ambientCommand.CommandType = CommandType.StoredProcedure;

            return (ambientCommand, null);
        }

        SqlConnection ownedConnection = _sqlConnectionFactory.CreateConnection();
        await ownedConnection.OpenAsync(cancellationToken);

        SqlCommand command = ownedConnection.CreateCommand();
        command.CommandText = storedProcedureName;
        command.CommandType = CommandType.StoredProcedure;

        return (command, ownedConnection);
    }

    private static EmailDeliveryListResultItem MapEmailDeliveryListItem(SqlDataReader reader)
    {
        string? toEmail = GetNullableString(reader, "ToEmail");

        return new EmailDeliveryListResultItem
        {
            EmailDeliveryId = reader.GetInt64(reader.GetOrdinal("EmailDeliveryId")),
            MessageId = reader.GetString(reader.GetOrdinal("MessageId")),
            RecipientUserId = GetNullableInt64(reader, "RecipientUserId"),
            MaskedToEmail = MaskEmail(toEmail),
            TemplateKey = reader.GetString(reader.GetOrdinal("TemplateKey")),
            Provider = reader.GetString(reader.GetOrdinal("Provider")),
            Status = reader.GetString(reader.GetOrdinal("Status")),
            AttemptCount = reader.GetInt32(reader.GetOrdinal("AttemptCount")),
            LastAttemptAt = GetNullableDateTime(reader, "LastAttemptAt"),
            NextRetryAt = GetNullableDateTime(reader, "NextRetryAt"),
            SentAt = GetNullableDateTime(reader, "SentAt"),
            LastErrorCode = GetNullableString(reader, "LastErrorCode"),
            LastErrorClass = GetNullableString(reader, "LastErrorClass"),
            CorrelationId = GetNullableString(reader, "CorrelationId"),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
        };
    }

    private static EmailDeliveryDetailResult MapEmailDeliveryDetail(SqlDataReader reader)
    {
        return new EmailDeliveryDetailResult
        {
            EmailDeliveryId = reader.GetInt64(reader.GetOrdinal("EmailDeliveryId")),
            MessageId = reader.GetString(reader.GetOrdinal("MessageId")),
            BusinessDedupeKey = reader.GetString(reader.GetOrdinal("BusinessDedupeKey")),
            RecipientUserId = GetNullableInt64(reader, "RecipientUserId"),
            ToEmail = reader.GetString(reader.GetOrdinal("ToEmail")),
            TemplateKey = reader.GetString(reader.GetOrdinal("TemplateKey")),
            Provider = reader.GetString(reader.GetOrdinal("Provider")),
            Status = reader.GetString(reader.GetOrdinal("Status")),
            AttemptCount = reader.GetInt32(reader.GetOrdinal("AttemptCount")),
            LastAttemptAt = GetNullableDateTime(reader, "LastAttemptAt"),
            NextRetryAt = GetNullableDateTime(reader, "NextRetryAt"),
            SentAt = GetNullableDateTime(reader, "SentAt"),
            LastErrorCode = GetNullableString(reader, "LastErrorCode"),
            LastErrorClass = GetNullableString(reader, "LastErrorClass"),
            CorrelationId = GetNullableString(reader, "CorrelationId"),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
        };
    }

    private static EmailDeliveryAttemptResultItem MapEmailDeliveryAttempt(SqlDataReader reader)
    {
        return new EmailDeliveryAttemptResultItem
        {
            EmailDeliveryAttemptId = reader.GetInt64(reader.GetOrdinal("EmailDeliveryAttemptId")),
            EmailDeliveryId = reader.GetInt64(reader.GetOrdinal("EmailDeliveryId")),
            AttemptNumber = reader.GetInt32(reader.GetOrdinal("AttemptNumber")),
            StartedAt = reader.GetDateTime(reader.GetOrdinal("StartedAt")),
            FinishedAt = GetNullableDateTime(reader, "FinishedAt"),
            Outcome = reader.GetString(reader.GetOrdinal("Outcome")),
            IsAmbiguous = reader.GetBoolean(reader.GetOrdinal("IsAmbiguous")),
            ProviderMessageId = GetNullableString(reader, "ProviderMessageId"),
            ProviderErrorCode = GetNullableString(reader, "ProviderErrorCode"),
            ErrorClass = GetNullableString(reader, "ErrorClass"),
            ErrorDetail = GetNullableString(reader, "ErrorDetail"),
            CorrelationId = GetNullableString(reader, "CorrelationId"),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }

    private static string? MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        int atIndex = email.IndexOf('@');
        if (atIndex <= 1 || atIndex == email.Length - 1)
        {
            return "***";
        }

        string local = email[..atIndex];
        string domain = email[(atIndex + 1)..];

        string maskedLocal = local.Length switch
        {
            <= 1 => "*",
            2 => $"{local[0]}*",
            _ => $"{local[0]}***{local[^1]}"
        };

        return $"{maskedLocal}@{domain}";
    }

    private static object ToDbValue(object? value) => value ?? DBNull.Value;

    private static string? GetNullableString(SqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static long? GetNullableInt64(SqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);
    }

    private static DateTime? GetNullableDateTime(SqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }
}