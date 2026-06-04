using System.Data;
using Audit.Application.Abstractions.Persistence;
using Audit.Application.Abstractions.Persistence.Queries;
using Audit.Application.Abstractions.Persistence.Results;
using Audit.Domain.Constants.AuditLog;
using Audit.Domain.Constants.Common;
using Audit.Domain.Entities;
using Audit.Infrastructure.Persistence.Exceptions;
using Audit.Infrastructure.Persistence.Mapping;
using Audit.Infrastructure.Persistence.Sql;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using Microsoft.Data.SqlClient;

namespace Audit.Infrastructure.Persistence.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly IAuditUnitOfWork _unitOfWork;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly AuditSqlExceptionTranslator _sqlExceptionTranslator;

    public AuditLogRepository(
        IAuditUnitOfWork unitOfWork,
        ISqlConnectionFactory sqlConnectionFactory,
        AuditSqlExceptionTranslator sqlExceptionTranslator)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
        _sqlExceptionTranslator = sqlExceptionTranslator ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
    }

    public async Task<AuditLogInsertResult> InsertAsync(
        AuditLog auditLog,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditLog);

        try
        {
            using SqlCommand command = CreateTransactionalStoredProcedureCommand(
                SqlStoredProcedures.AuditLog.Insert);

            SqlParameter auditLogIdParameter = SqlParameterFactory.OutputBigInt(
                SqlParameterNames.AuditLog.AuditLogId);

            SqlParameter wasInsertedParameter = SqlParameterFactory.OutputBit(
                SqlParameterNames.AuditLog.WasInserted);

            AddInsertParameters(
                command,
                auditLog,
                auditLogIdParameter,
                wasInsertedParameter);

            await command.ExecuteNonQueryAsync(cancellationToken);

            return new AuditLogInsertResult(
                AuditLogId: auditLogIdParameter.GetRequiredInt64Value(),
                WasInserted: wasInsertedParameter.GetRequiredBooleanValue());
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<AuditLog?> GetByPublicIdAsync(
        string publicId,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteSingleAsync(
            SqlStoredProcedures.AuditLog.SelectByPublicId,
            command => command.Parameters.Add(
                SqlParameterFactory.Char(
                    SqlParameterNames.Common.PublicId,
                    publicId,
                    AuditConstants.PublicIdLength)),
            cancellationToken);
    }

    public async Task<AuditLog?> GetByMessageIdAsync(
        string messageId,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteSingleAsync(
            SqlStoredProcedures.AuditLog.SelectByMessageId,
            command => command.Parameters.Add(
                SqlParameterFactory.Char(
                    SqlParameterNames.Common.MessageId,
                    messageId,
                    AuditConstants.MessageIdLength)),
            cancellationToken);
    }

    public async Task<PagedQueryResult<AuditLog>> SearchAsync(
        AuditLogSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        int skip = (query.Page - 1) * query.PageSize;

        IReadOnlyList<AuditLog> items = await ExecuteListAsync(
            SqlStoredProcedures.AuditLog.SelectSkipAndTakeWhereDynamic,
            command => AddAuditLogSearchParameters(command, query, skip),
            cancellationToken);

        int totalItems = await CountAsync(query, cancellationToken);

        return new PagedQueryResult<AuditLog>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalItems = totalItems
        };
    }

    public async Task<PagedQueryResult<AuditLog>> GetByCorrelationIdAsync(
        string correlationId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await SearchAsync(
            new AuditLogSearchQuery(
                MessageId: null,
                SourceModule: null,
                EventType: null,
                Action: null,
                ActionCategory: null,
                ResourceType: null,
                ResourceId: null,
                ActorUserId: null,
                ActorInternalId: null,
                Outcome: null,
                Severity: null,
                RiskLevel: null,
                CorrelationId: correlationId,
                FromUtc: fromUtc,
                ToUtc: toUtc,
                Page: page,
                PageSize: pageSize,
                SortBy: null,
                SortDirection: null),
            cancellationToken);
    }

    public async Task<PagedQueryResult<AuditLog>> GetResourceTimelineAsync(
        string resourceType,
        string resourceId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await SearchAsync(
            new AuditLogSearchQuery(
                MessageId: null,
                SourceModule: null,
                EventType: null,
                Action: null,
                ActionCategory: null,
                ResourceType: resourceType,
                ResourceId: resourceId,
                ActorUserId: null,
                ActorInternalId: null,
                Outcome: null,
                Severity: null,
                RiskLevel: null,
                CorrelationId: null,
                FromUtc: fromUtc,
                ToUtc: toUtc,
                Page: page,
                PageSize: pageSize,
                SortBy: null,
                SortDirection: null),
            cancellationToken);
    }

    public async Task<PagedQueryResult<AuditLog>> GetActorTimelineAsync(
        string actorUserId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await SearchAsync(
            new AuditLogSearchQuery(
                MessageId: null,
                SourceModule: null,
                EventType: null,
                Action: null,
                ActionCategory: null,
                ResourceType: null,
                ResourceId: null,
                ActorUserId: actorUserId,
                ActorInternalId: null,
                Outcome: null,
                Severity: null,
                RiskLevel: null,
                CorrelationId: null,
                FromUtc: fromUtc,
                ToUtc: toUtc,
                Page: page,
                PageSize: pageSize,
                SortBy: null,
                SortDirection: null),
            cancellationToken);
    }

    public async Task<int> CountAsync(
        AuditLogSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await ExecuteScalarIntAsync(
            SqlStoredProcedures.AuditLog.GetRecordCountWhereDynamic,
            command => AddAuditLogCountParameters(command, query),
            cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLog>> GetRecentRiskEventsAsync(
        AuditRecentRiskEventsSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await ExecuteListAsync(
            SqlStoredProcedures.AuditLog.SelectRecentRiskEvents,
            command => AddRecentRiskParameters(command, query),
            cancellationToken);
    }

    public async Task<int> CountHighRiskAsync(
        AuditDashboardSummarySearchQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await CountDashboardRiskLevelAsync(
            query,
            AuditRiskLevels.High,
            cancellationToken);
    }

    public async Task<int> CountCriticalAsync(
        AuditDashboardSummarySearchQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await CountDashboardRiskLevelAsync(
            query,
            AuditRiskLevels.Critical,
            cancellationToken);
    }

    public async Task<IReadOnlyList<AuditCountByValueResult>> CountByModuleAsync(
        AuditDashboardSummarySearchQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await ExecuteCountByValueListAsync(
            SqlStoredProcedures.AuditLog.CountByModule,
            command => AddDashboardFilterParameters(command, query),
            cancellationToken);
    }

    public async Task<IReadOnlyList<AuditCountByValueResult>> CountBySeverityAsync(
        AuditDashboardSummarySearchQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await ExecuteCountByValueListAsync(
            SqlStoredProcedures.AuditLog.CountBySeverity,
            command => AddDashboardFilterParameters(command, query),
            cancellationToken);
    }

    public async Task<IReadOnlyList<AuditCountByValueResult>> CountByRiskLevelAsync(
        AuditDashboardSummarySearchQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await ExecuteCountByValueListAsync(
            SqlStoredProcedures.AuditLog.CountByRiskLevel,
            command => AddDashboardFilterParameters(command, query),
            cancellationToken);
    }

    private static void AddInsertParameters(
        SqlCommand command,
        AuditLog auditLog,
        SqlParameter auditLogIdParameter,
        SqlParameter wasInsertedParameter)
    {
        command.Parameters.AddRange(
        [
            SqlParameterFactory.Char(SqlParameterNames.Common.PublicId, auditLog.PublicId, AuditConstants.PublicIdLength),
            SqlParameterFactory.Char(SqlParameterNames.Common.MessageId, auditLog.SourceEvent.MessageId, AuditConstants.MessageIdLength),
            SqlParameterFactory.NVarChar(SqlParameterNames.Common.EventType, auditLog.SourceEvent.EventType, AuditConstants.MaxEventTypeLength),
            SqlParameterFactory.Int(SqlParameterNames.Common.EventVersion, auditLog.SourceEvent.EventVersion),
            SqlParameterFactory.NVarChar(SqlParameterNames.Common.SourceModule, auditLog.SourceEvent.SourceModule, AuditConstants.MaxSourceModuleLength),
            SqlParameterFactory.NVarChar(SqlParameterNames.AuditLog.Action, auditLog.Action, AuditConstants.MaxActionLength),
            SqlParameterFactory.NVarChar(SqlParameterNames.AuditLog.ActionCategory, auditLog.ActionCategory, AuditConstants.MaxActionCategoryLength),
            SqlParameterFactory.NVarChar(SqlParameterNames.Common.AggregateType, auditLog.AggregateRef.AggregateType, AuditConstants.MaxAggregateTypeLength),
            SqlParameterFactory.NVarChar(SqlParameterNames.Common.AggregateId, auditLog.AggregateRef.AggregateId, AuditConstants.MaxAggregateIdLength),
            SqlParameterFactory.Char(SqlParameterNames.Common.AggregatePublicId, auditLog.AggregateRef.AggregatePublicId, AuditConstants.PublicIdLength),
            SqlParameterFactory.Int(SqlParameterNames.Common.AggregateVersion, auditLog.AggregateRef.AggregateVersion),
            SqlParameterFactory.NVarChar(SqlParameterNames.AuditLog.ResourceType, auditLog.Resource.ResourceType, AuditConstants.MaxResourceTypeLength),
            SqlParameterFactory.NVarChar(SqlParameterNames.AuditLog.ResourceId, auditLog.Resource.ResourceId, AuditConstants.MaxResourceIdLength),
            SqlParameterFactory.NVarChar(SqlParameterNames.AuditLog.ResourceDisplayName, auditLog.Resource.ResourceDisplayName, AuditConstants.MaxResourceDisplayNameLength),
            SqlParameterFactory.BigInt(SqlParameterNames.AuditLog.ActorInternalId, auditLog.Actor.ActorInternalId),
            SqlParameterFactory.Char(SqlParameterNames.AuditLog.ActorUserId, auditLog.Actor.ActorUserId, AuditConstants.PublicIdLength),
            SqlParameterFactory.NVarChar(SqlParameterNames.AuditLog.ActorEmail, auditLog.Actor.ActorEmail, AuditConstants.MaxActorEmailLength),
            SqlParameterFactory.NVarChar(SqlParameterNames.AuditLog.ActorDisplayName, auditLog.Actor.ActorDisplayName, AuditConstants.MaxActorDisplayNameLength),
            SqlParameterFactory.VarChar(SqlParameterNames.AuditLog.ActorType, auditLog.Actor.ActorType, 30),
            SqlParameterFactory.VarChar(SqlParameterNames.AuditLog.Outcome, auditLog.Risk.Outcome, 30),
            SqlParameterFactory.VarChar(SqlParameterNames.AuditLog.Severity, auditLog.Risk.Severity, 30),
            SqlParameterFactory.VarChar(SqlParameterNames.AuditLog.RiskLevel, auditLog.Risk.RiskLevel, 30),
            SqlParameterFactory.NVarChar(SqlParameterNames.AuditLog.Summary, auditLog.Summary, AuditConstants.MaxSummaryLength),
            SqlParameterFactory.NVarChar(SqlParameterNames.AuditLog.Reason, auditLog.Reason, AuditConstants.MaxReasonLength),
            SqlParameterFactory.NVarChar(SqlParameterNames.Common.CorrelationId, auditLog.TraceContext.CorrelationId, AuditConstants.MaxCorrelationIdLength),
            SqlParameterFactory.NVarChar(SqlParameterNames.Common.CausationId, auditLog.TraceContext.CausationId, AuditConstants.MaxCausationIdLength),
            SqlParameterFactory.NVarChar(SqlParameterNames.Common.TraceId, auditLog.TraceContext.TraceId, AuditConstants.MaxTraceIdLength),
            SqlParameterFactory.NVarChar(SqlParameterNames.AuditLog.IpAddress, auditLog.RequestContext.IpAddress, AuditConstants.MaxIpAddressLength),
            SqlParameterFactory.NVarChar(SqlParameterNames.AuditLog.UserAgent, auditLog.RequestContext.UserAgent, AuditConstants.MaxUserAgentLength),
            SqlParameterFactory.TinyInt(SqlParameterNames.Common.SourcePriority, auditLog.SourceEvent.SourcePriority),
            SqlParameterFactory.DateTime2(SqlParameterNames.AuditLog.OccurredAtUtc, auditLog.OccurredAtUtc),
            SqlParameterFactory.NVarCharMax(SqlParameterNames.AuditLog.MetadataJson, auditLog.JsonPayload.MetadataJson),
            SqlParameterFactory.NVarCharMax(SqlParameterNames.AuditLog.HeadersJson, auditLog.JsonPayload.HeadersJson),
            SqlParameterFactory.NVarCharMax(SqlParameterNames.AuditLog.SanitizedPayloadJson, auditLog.JsonPayload.SanitizedPayloadJson),
            SqlParameterFactory.NVarCharMax(SqlParameterNames.AuditLog.BeforeJson, auditLog.JsonPayload.BeforeJson),
            SqlParameterFactory.NVarCharMax(SqlParameterNames.AuditLog.AfterJson, auditLog.JsonPayload.AfterJson),
            SqlParameterFactory.NVarCharMax(SqlParameterNames.AuditLog.ChangesJson, auditLog.JsonPayload.ChangesJson),
            SqlParameterFactory.Char(SqlParameterNames.AuditLog.Hash, null, AuditConstants.HashLength),
            SqlParameterFactory.Char(SqlParameterNames.AuditLog.PrevHash, null, AuditConstants.HashLength),
            SqlParameterFactory.DateTime2(SqlParameterNames.AuditLog.IngestedAtUtc, auditLog.IngestedAtUtc),
            SqlParameterFactory.DateTime2(SqlParameterNames.AuditLog.CreatedAtUtc, auditLog.CreatedAtUtc),
            auditLogIdParameter,
            wasInsertedParameter
        ]);
    }

    private static void AddAuditLogSearchParameters(
        SqlCommand command,
        AuditLogSearchQuery query,
        int skip)
    {
        AddAuditLogCountParameters(command, query);
        command.Parameters.Add(SqlParameterFactory.NVarChar(SqlParameterNames.Common.SortBy, query.SortBy, 100));
        command.Parameters.Add(SqlParameterFactory.VarChar(SqlParameterNames.Common.SortDirection, query.SortDirection, 4));
        command.Parameters.Add(SqlParameterFactory.Int(SqlParameterNames.Common.Skip, skip));
        command.Parameters.Add(SqlParameterFactory.Int(SqlParameterNames.Common.Take, query.PageSize));
    }

    private static void AddAuditLogCountParameters(
        SqlCommand command,
        AuditLogSearchQuery query)
    {
        command.Parameters.AddRange(
        [
            SqlParameterFactory.DateTime2(SqlParameterNames.Common.FromOccurredAtUtc, query.FromUtc),
            SqlParameterFactory.DateTime2(SqlParameterNames.Common.ToOccurredAtUtc, query.ToUtc),
            SqlParameterFactory.NVarChar(SqlParameterNames.Common.SourceModule, query.SourceModule, AuditConstants.MaxSourceModuleLength),
            SqlParameterFactory.NVarChar(SqlParameterNames.Common.EventType, query.EventType, AuditConstants.MaxEventTypeLength),
            SqlParameterFactory.NVarChar(SqlParameterNames.AuditLog.Action, query.Action, AuditConstants.MaxActionLength),
            SqlParameterFactory.NVarChar(SqlParameterNames.AuditLog.ActionCategory, query.ActionCategory, AuditConstants.MaxActionCategoryLength),
            SqlParameterFactory.Char(SqlParameterNames.AuditLog.ActorUserId, query.ActorUserId, AuditConstants.PublicIdLength),
            SqlParameterFactory.BigInt(SqlParameterNames.AuditLog.ActorInternalId, query.ActorInternalId),
            SqlParameterFactory.NVarChar(SqlParameterNames.AuditLog.ResourceType, query.ResourceType, AuditConstants.MaxResourceTypeLength),
            SqlParameterFactory.NVarChar(SqlParameterNames.AuditLog.ResourceId, query.ResourceId, AuditConstants.MaxResourceIdLength),
            SqlParameterFactory.NVarChar(SqlParameterNames.Common.CorrelationId, query.CorrelationId, AuditConstants.MaxCorrelationIdLength),
            SqlParameterFactory.Char(SqlParameterNames.Common.MessageId, query.MessageId, AuditConstants.MessageIdLength),
            SqlParameterFactory.VarChar(SqlParameterNames.AuditLog.Outcome, query.Outcome, 30),
            SqlParameterFactory.VarChar(SqlParameterNames.AuditLog.Severity, query.Severity, 30),
            SqlParameterFactory.VarChar(SqlParameterNames.AuditLog.RiskLevel, query.RiskLevel, 30)
        ]);
    }

    private static void AddRecentRiskParameters(
        SqlCommand command,
        AuditRecentRiskEventsSearchQuery query)
    {
        command.Parameters.AddRange(
        [
            SqlParameterFactory.DateTime2(SqlParameterNames.Common.FromOccurredAtUtc, query.FromUtc),
            SqlParameterFactory.DateTime2(SqlParameterNames.Common.ToOccurredAtUtc, query.ToUtc),
            SqlParameterFactory.NVarChar(SqlParameterNames.Common.SourceModule, query.SourceModule, AuditConstants.MaxSourceModuleLength),
            SqlParameterFactory.VarChar(SqlParameterNames.AuditLog.RiskLevel, query.RiskLevel, 30),
            SqlParameterFactory.Int(SqlParameterNames.Common.Limit, query.Limit)
        ]);
    }

    private async Task<int> CountDashboardRiskLevelAsync(
        AuditDashboardSummarySearchQuery query,
        string riskLevel,
        CancellationToken cancellationToken)
    {
        return await ExecuteScalarIntAsync(
            SqlStoredProcedures.AuditLog.GetRecordCountWhereDynamic,
            command => AddDashboardRiskCountParameters(command, query, riskLevel),
            cancellationToken);
    }

    private static void AddDashboardRiskCountParameters(
        SqlCommand command,
        AuditDashboardSummarySearchQuery query,
        string riskLevel)
    {
        command.Parameters.AddRange(
        [
            SqlParameterFactory.DateTime2(SqlParameterNames.Common.FromOccurredAtUtc, query.FromUtc),
            SqlParameterFactory.DateTime2(SqlParameterNames.Common.ToOccurredAtUtc, query.ToUtc),
            SqlParameterFactory.NVarChar(SqlParameterNames.Common.SourceModule, query.SourceModule, AuditConstants.MaxSourceModuleLength),
            SqlParameterFactory.NVarChar(SqlParameterNames.Common.EventType, null, AuditConstants.MaxEventTypeLength),
            SqlParameterFactory.NVarChar(SqlParameterNames.AuditLog.Action, null, AuditConstants.MaxActionLength),
            SqlParameterFactory.NVarChar(SqlParameterNames.AuditLog.ActionCategory, null, AuditConstants.MaxActionCategoryLength),
            SqlParameterFactory.Char(SqlParameterNames.AuditLog.ActorUserId, null, AuditConstants.PublicIdLength),
            SqlParameterFactory.BigInt(SqlParameterNames.AuditLog.ActorInternalId, null),
            SqlParameterFactory.NVarChar(SqlParameterNames.AuditLog.ResourceType, null, AuditConstants.MaxResourceTypeLength),
            SqlParameterFactory.NVarChar(SqlParameterNames.AuditLog.ResourceId, null, AuditConstants.MaxResourceIdLength),
            SqlParameterFactory.NVarChar(SqlParameterNames.Common.CorrelationId, null, AuditConstants.MaxCorrelationIdLength),
            SqlParameterFactory.Char(SqlParameterNames.Common.MessageId, null, AuditConstants.MessageIdLength),
            SqlParameterFactory.VarChar(SqlParameterNames.AuditLog.Outcome, null, 30),
            SqlParameterFactory.VarChar(SqlParameterNames.AuditLog.Severity, null, 30),
            SqlParameterFactory.VarChar(SqlParameterNames.AuditLog.RiskLevel, riskLevel, 30)
        ]);
    }

    private static void AddDashboardFilterParameters(
        SqlCommand command,
        AuditDashboardSummarySearchQuery query)
    {
        command.Parameters.AddRange(
        [
            SqlParameterFactory.DateTime2(SqlParameterNames.Common.FromOccurredAtUtc, query.FromUtc),
            SqlParameterFactory.DateTime2(SqlParameterNames.Common.ToOccurredAtUtc, query.ToUtc),
            SqlParameterFactory.NVarChar(SqlParameterNames.Common.SourceModule, query.SourceModule, AuditConstants.MaxSourceModuleLength)
        ]);
    }

    private async Task<AuditLog?> ExecuteSingleAsync(
        string storedProcedureName,
        Action<SqlCommand> configure,
        CancellationToken cancellationToken)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateCommandAsync(storedProcedureName, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                configure(command);

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                return await reader.ReadAsync(cancellationToken)
                    ? AuditLogDataMapper.Map(reader)
                    : null;
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

    private async Task<IReadOnlyList<AuditLog>> ExecuteListAsync(
        string storedProcedureName,
        Action<SqlCommand> configure,
        CancellationToken cancellationToken)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateCommandAsync(storedProcedureName, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                configure(command);

                return await ReadAuditLogsAsync(command, cancellationToken);
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

    private async Task<IReadOnlyList<AuditCountByValueResult>> ExecuteCountByValueListAsync(
        string storedProcedureName,
        Action<SqlCommand> configure,
        CancellationToken cancellationToken)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateCommandAsync(storedProcedureName, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                configure(command);

                List<AuditCountByValueResult> results = [];

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    results.Add(
                        new AuditCountByValueResult(
                            Value: reader.GetRequiredString("Value"),
                            Count: reader.GetRequiredInt32FromNumber("Count")));
                }

                return results;
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

    private async Task<int> ExecuteScalarIntAsync(
        string storedProcedureName,
        Action<SqlCommand> configure,
        CancellationToken cancellationToken)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateCommandAsync(storedProcedureName, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                configure(command);

                object? scalar = await command.ExecuteScalarAsync(cancellationToken);

                return SqlScalarValue.ToInt32OrDefault(scalar);
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

    private static async Task<IReadOnlyList<AuditLog>> ReadAuditLogsAsync(
        SqlCommand command,
        CancellationToken cancellationToken)
    {
        List<AuditLog> auditLogs = [];

        using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            auditLogs.Add(AuditLogDataMapper.Map(reader));
        }

        return auditLogs;
    }

    private SqlCommand CreateTransactionalStoredProcedureCommand(
        string storedProcedureName)
    {
        SqlCommand command = _unitOfWork.Connection.CreateCommand();
        command.Transaction = _unitOfWork.Transaction;
        command.CommandText = storedProcedureName;
        command.CommandType = CommandType.StoredProcedure;

        return command;
    }

    private async Task<(SqlCommand Command, SqlConnection? OwnedConnection)> CreateCommandAsync(
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
}
