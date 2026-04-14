using System.Data;
using Audit.Application.Models;
using Audit.Application.Models.QueryModels;
using Audit.Application.Ports.Persistence;
using Audit.Domain.Entities;
using Audit.Infrastructure.Persistence.Exceptions;
using Audit.Infrastructure.Persistence.Sql;
using CommercialNews.BuildingBlocks.Contracts.Common;
using CommercialNews.BuildingBlocks.Persistence.Sql;
using Microsoft.Data.SqlClient;

namespace Audit.Infrastructure.Persistence.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private const string AuditLogInsertProc =
        "[audit].[AuditLog_Insert]";

    private const string AuditLogSelectByIdProc =
        "[audit].[AuditLog_SelectById]";

    private const string AuditLogSelectByAuditEventIdProc =
        "[audit].[AuditLog_SelectByAuditEventId]";

    private const string AuditLogSelectSkipAndTakeWhereDynamicProc =
        "[audit].[AuditLog_SelectSkipAndTakeWhereDynamic]";

    private const string AuditLogSelectByCorrelationIdProc =
        "[audit].[AuditLog_SelectByCorrelationId]";

    private const string AuditLogGetRecordCountProc =
        "[audit].[AuditLog_GetRecordCount]";

    private const string AuditLogGetRecordCountWhereDynamicProc =
        "[audit].[AuditLog_GetRecordCountWhereDynamic]";

    private readonly AuditUnitOfWork _unitOfWork;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly AuditSqlExceptionTranslator _sqlExceptionTranslator;

    public AuditLogRepository(
        AuditUnitOfWork unitOfWork,
        ISqlConnectionFactory sqlConnectionFactory,
        AuditSqlExceptionTranslator sqlExceptionTranslator)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
        _sqlExceptionTranslator = sqlExceptionTranslator ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
    }

    public async Task<AuditInsertResult> InsertIfNotExistsAsync(
        AuditLog auditLog,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditLog);

        try
        {
            using SqlCommand command = CreateTransactionalCommand(AuditLogInsertProc);

            SqlParameter auditIdParameter = new("@AuditId", SqlDbType.BigInt)
            {
                Direction = ParameterDirection.Output
            };

            SqlParameter wasInsertedParameter = new("@WasInserted", SqlDbType.Bit)
            {
                Direction = ParameterDirection.Output
            };

            command.Parameters.AddRange(
            [
                new SqlParameter("@AuditEventId", SqlDbType.Char, 26) { Value = auditLog.AuditEventId },
                new SqlParameter("@ActorUserId", SqlDbType.BigInt) { Value = ToDbValue(auditLog.ActorUserId) },
                new SqlParameter("@Action", SqlDbType.NVarChar, 120) { Value = auditLog.Action },
                new SqlParameter("@ResourceType", SqlDbType.NVarChar, 60) { Value = auditLog.ResourceType },
                new SqlParameter("@ResourceId", SqlDbType.NVarChar, 100) { Value = auditLog.ResourceId },
                new SqlParameter("@Outcome", SqlDbType.NVarChar, 30) { Value = ToDbValue(auditLog.Outcome) },
                new SqlParameter("@Summary", SqlDbType.NVarChar, 300) { Value = auditLog.Summary },
                new SqlParameter("@Reason", SqlDbType.NVarChar, 500) { Value = ToDbValue(auditLog.Reason) },
                new SqlParameter("@OccurredAt", SqlDbType.DateTime2) { Value = auditLog.OccurredAt },
                new SqlParameter("@CorrelationId", SqlDbType.NVarChar, 100) { Value = ToDbValue(auditLog.CorrelationId) },
                new SqlParameter("@IpAddress", SqlDbType.NVarChar, 45) { Value = ToDbValue(auditLog.IpAddress) },
                new SqlParameter("@UserAgent", SqlDbType.NVarChar, 300) { Value = ToDbValue(auditLog.UserAgent) },
                new SqlParameter("@OldValuesJson", SqlDbType.NVarChar) { Value = ToDbValue(auditLog.OldValuesJson) },
                new SqlParameter("@NewValuesJson", SqlDbType.NVarChar) { Value = ToDbValue(auditLog.NewValuesJson) },
                new SqlParameter("@MetadataJson", SqlDbType.NVarChar) { Value = ToDbValue(auditLog.MetadataJson) },
                auditIdParameter,
                wasInsertedParameter
            ]);

            await command.ExecuteNonQueryAsync(cancellationToken);

            long auditId = auditIdParameter.Value is DBNull
                ? 0
                : Convert.ToInt64(auditIdParameter.Value);

            bool wasInserted = wasInsertedParameter.Value is not DBNull
                               && Convert.ToBoolean(wasInsertedParameter.Value);

            return new AuditInsertResult
            {
                AuditId = auditId,
                WasInserted = wasInserted
            };
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<AuditLog?> GetByIdAsync(
        long auditId,
        CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(AuditLogSelectByIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@AuditId", SqlDbType.BigInt) { Value = auditId });

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return MapAuditLog(reader);
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

    public async Task<AuditLog?> GetByAuditEventIdAsync(
        string auditEventId,
        CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(AuditLogSelectByAuditEventIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@AuditEventId", SqlDbType.Char, 26) { Value = auditEventId });

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return MapAuditLog(reader);
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

    public async Task<AuditLogDetailResult?> SelectDetailByIdAsync(
        long auditId,
        CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(AuditLogSelectByIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@AuditId", SqlDbType.BigInt) { Value = auditId });

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return MapAuditLogDetailResult(reader);
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

    public async Task<AuditLogDetailResult?> SelectDetailByAuditEventIdAsync(
        string auditEventId,
        CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(AuditLogSelectByAuditEventIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@AuditEventId", SqlDbType.Char, 26) { Value = auditEventId });

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return MapAuditLogDetailResult(reader);
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

    public async Task<PagedQueryResult<AuditLogListResultItem>> SelectSkipAndTakeAsync(
        AuditLogListQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        SqlConnection? ownedConnection = null;

        try
        {
            int page = (query.Skip / query.Take) + 1;

            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(AuditLogSelectSkipAndTakeWhereDynamicProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.AddRange(
                [
                    new SqlParameter("@FromOccurredAt", SqlDbType.DateTime2) { Value = ToDbValue(query.FromOccurredAt) },
                    new SqlParameter("@ToOccurredAt", SqlDbType.DateTime2) { Value = ToDbValue(query.ToOccurredAt) },
                    new SqlParameter("@ActorUserId", SqlDbType.BigInt) { Value = ToDbValue(query.ActorUserId) },
                    new SqlParameter("@Action", SqlDbType.NVarChar, 120) { Value = ToDbValue(query.Action) },
                    new SqlParameter("@ResourceType", SqlDbType.NVarChar, 60) { Value = ToDbValue(query.ResourceType) },
                    new SqlParameter("@ResourceId", SqlDbType.NVarChar, 100) { Value = ToDbValue(query.ResourceId) },
                    new SqlParameter("@CorrelationId", SqlDbType.NVarChar, 100) { Value = ToDbValue(query.CorrelationId) },
                    new SqlParameter("@AuditEventId", SqlDbType.Char, 26) { Value = ToDbValue(query.AuditEventId) },
                    new SqlParameter("@Outcome", SqlDbType.NVarChar, 30) { Value = ToDbValue(query.Outcome) },
                    new SqlParameter("@Skip", SqlDbType.Int) { Value = query.Skip },
                    new SqlParameter("@Take", SqlDbType.Int) { Value = query.Take }
                ]);

                List<AuditLogListResultItem> items = [];

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    items.Add(MapAuditLogListResultItem(reader));
                }

                int totalItems = await GetRecordCountAsync(query, cancellationToken);

                return new PagedQueryResult<AuditLogListResultItem>
                {
                    Items = items,
                    Page = page,
                    PageSize = query.Take,
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

    public async Task<PagedQueryResult<AuditLogListResultItem>> SelectByCorrelationIdAsync(
        AuditLogByCorrelationQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        SqlConnection? ownedConnection = null;

        try
        {
            int page = (query.Skip / query.Take) + 1;

            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(AuditLogSelectByCorrelationIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.AddRange(
                [
                    new SqlParameter("@CorrelationId", SqlDbType.NVarChar, 100) { Value = query.CorrelationId },
                    new SqlParameter("@Skip", SqlDbType.Int) { Value = query.Skip },
                    new SqlParameter("@Take", SqlDbType.Int) { Value = query.Take }
                ]);

                List<AuditLogListResultItem> items = [];

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    items.Add(MapAuditLogListResultItem(reader));
                }

                int totalItems = await GetRecordCountByCorrelationIdAsync(
                    query.CorrelationId,
                    cancellationToken);

                return new PagedQueryResult<AuditLogListResultItem>
                {
                    Items = items,
                    Page = page,
                    PageSize = query.Take,
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

    private SqlCommand CreateTransactionalCommand(string storedProcedureName)
    {
        SqlCommand command = _unitOfWork.Connection.CreateCommand();
        command.Transaction = _unitOfWork.Transaction;
        command.CommandText = storedProcedureName;
        command.CommandType = CommandType.StoredProcedure;
        return command;
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

    private async Task<int> GetRecordCountAsync(
        AuditLogListQuery query,
        CancellationToken cancellationToken)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(AuditLogGetRecordCountWhereDynamicProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.AddRange(
                [
                    new SqlParameter("@FromOccurredAt", SqlDbType.DateTime2) { Value = ToDbValue(query.FromOccurredAt) },
                    new SqlParameter("@ToOccurredAt", SqlDbType.DateTime2) { Value = ToDbValue(query.ToOccurredAt) },
                    new SqlParameter("@ActorUserId", SqlDbType.BigInt) { Value = ToDbValue(query.ActorUserId) },
                    new SqlParameter("@Action", SqlDbType.NVarChar, 120) { Value = ToDbValue(query.Action) },
                    new SqlParameter("@ResourceType", SqlDbType.NVarChar, 60) { Value = ToDbValue(query.ResourceType) },
                    new SqlParameter("@ResourceId", SqlDbType.NVarChar, 100) { Value = ToDbValue(query.ResourceId) },
                    new SqlParameter("@CorrelationId", SqlDbType.NVarChar, 100) { Value = ToDbValue(query.CorrelationId) },
                    new SqlParameter("@AuditEventId", SqlDbType.Char, 26) { Value = ToDbValue(query.AuditEventId) },
                    new SqlParameter("@Outcome", SqlDbType.NVarChar, 30) { Value = ToDbValue(query.Outcome) }
                ]);

                object? scalar = await command.ExecuteScalarAsync(cancellationToken);

                return scalar is null or DBNull
                    ? 0
                    : Convert.ToInt32(scalar);
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

    private async Task<int> GetRecordCountByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken)
    {
        AuditLogListQuery query = new()
        {
            CorrelationId = correlationId,
            Skip = 0,
            Take = 1
        };

        return await GetRecordCountAsync(query, cancellationToken);
    }

    private static AuditLog MapAuditLog(SqlDataReader reader)
    {
        return AuditLog.Rehydrate(
            auditId: reader.GetInt64(reader.GetOrdinal("AuditId")),
            auditEventId: reader.GetString(reader.GetOrdinal("AuditEventId")),
            actorUserId: GetNullableInt64(reader, "ActorUserId"),
            action: reader.GetString(reader.GetOrdinal("Action")),
            resourceType: reader.GetString(reader.GetOrdinal("ResourceType")),
            resourceId: reader.GetString(reader.GetOrdinal("ResourceId")),
            outcome: GetNullableString(reader, "Outcome"),
            summary: reader.GetString(reader.GetOrdinal("Summary")),
            reason: GetNullableString(reader, "Reason"),
            occurredAt: reader.GetDateTime(reader.GetOrdinal("OccurredAt")),
            correlationId: GetNullableString(reader, "CorrelationId"),
            ipAddress: GetNullableString(reader, "IpAddress"),
            userAgent: GetNullableString(reader, "UserAgent"),
            oldValuesJson: GetNullableString(reader, "OldValuesJson"),
            newValuesJson: GetNullableString(reader, "NewValuesJson"),
            metadataJson: GetNullableString(reader, "MetadataJson"));
    }

    private static AuditLogDetailResult MapAuditLogDetailResult(SqlDataReader reader)
    {
        return new AuditLogDetailResult
        {
            AuditId = reader.GetInt64(reader.GetOrdinal("AuditId")),
            AuditEventId = reader.GetString(reader.GetOrdinal("AuditEventId")),
            OccurredAt = reader.GetDateTime(reader.GetOrdinal("OccurredAt")),
            ActorUserId = GetNullableInt64(reader, "ActorUserId"),
            Action = reader.GetString(reader.GetOrdinal("Action")),
            ResourceType = reader.GetString(reader.GetOrdinal("ResourceType")),
            ResourceId = reader.GetString(reader.GetOrdinal("ResourceId")),
            Outcome = GetNullableString(reader, "Outcome"),
            Summary = reader.GetString(reader.GetOrdinal("Summary")),
            Reason = GetNullableString(reader, "Reason"),
            CorrelationId = GetNullableString(reader, "CorrelationId"),
            IpAddress = GetNullableString(reader, "IpAddress"),
            UserAgent = GetNullableString(reader, "UserAgent"),
            OldValuesJson = GetNullableString(reader, "OldValuesJson"),
            NewValuesJson = GetNullableString(reader, "NewValuesJson"),
            MetadataJson = GetNullableString(reader, "MetadataJson")
        };
    }

    private static AuditLogListResultItem MapAuditLogListResultItem(SqlDataReader reader)
    {
        return new AuditLogListResultItem
        {
            AuditId = reader.GetInt64(reader.GetOrdinal("AuditId")),
            AuditEventId = reader.GetString(reader.GetOrdinal("AuditEventId")),
            OccurredAt = reader.GetDateTime(reader.GetOrdinal("OccurredAt")),
            ActorUserId = GetNullableInt64(reader, "ActorUserId"),
            Action = reader.GetString(reader.GetOrdinal("Action")),
            ResourceType = reader.GetString(reader.GetOrdinal("ResourceType")),
            ResourceId = reader.GetString(reader.GetOrdinal("ResourceId")),
            Outcome = GetNullableString(reader, "Outcome"),
            Summary = reader.GetString(reader.GetOrdinal("Summary")),
            CorrelationId = GetNullableString(reader, "CorrelationId")
        };
    }

    private static object ToDbValue(object? value) => value ?? DBNull.Value;

    private static long? GetNullableInt64(SqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);
    }

    private static string? GetNullableString(SqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}