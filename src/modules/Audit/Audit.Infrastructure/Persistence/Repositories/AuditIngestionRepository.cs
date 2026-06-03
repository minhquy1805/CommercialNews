using System.Data;
using Audit.Application.Abstractions.Persistence;
using Audit.Application.Abstractions.Persistence.Commands;
using Audit.Application.Abstractions.Persistence.Queries;
using Audit.Application.Abstractions.Persistence.Results;
using Audit.Domain.Constants.Common;
using Audit.Domain.Entities;
using Audit.Domain.ValueObjects.Ingestion;
using Audit.Infrastructure.Persistence.Exceptions;
using Audit.Infrastructure.Persistence.Mapping;
using Audit.Infrastructure.Persistence.Sql;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using Microsoft.Data.SqlClient;

namespace Audit.Infrastructure.Persistence.Repositories;

public sealed class AuditIngestionRepository : IAuditIngestionRepository
{
    private readonly IAuditUnitOfWork _unitOfWork;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly AuditSqlExceptionTranslator _sqlExceptionTranslator;

    public AuditIngestionRepository(
        IAuditUnitOfWork unitOfWork,
        ISqlConnectionFactory sqlConnectionFactory,
        AuditSqlExceptionTranslator sqlExceptionTranslator)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
        _sqlExceptionTranslator = sqlExceptionTranslator ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
    }

    public async Task<AuditIngestionUpsertResult> UpsertProcessingAsync(
        AuditIngestion auditIngestion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditIngestion);

        return await ExecuteUpsertAsync(
            SqlStoredProcedures.AuditIngestion.UpsertProcessing,
            command => AddUpsertProcessingParameters(command, auditIngestion),
            cancellationToken);
    }

    public async Task<AuditIngestionUpsertResult> UpsertUnsupportedAsync(
        AuditUnsupportedIngestionUpsertCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        return await ExecuteUpsertAsync(
            SqlStoredProcedures.AuditIngestion.UpsertUnsupported,
            sqlCommand => AddUpsertUnsupportedParameters(sqlCommand, command),
            cancellationToken);
    }

    public async Task MarkSucceededAsync(
        string messageId,
        CancellationToken cancellationToken = default)
    {
        await ExecuteMarkAsync(
            SqlStoredProcedures.AuditIngestion.MarkSucceeded,
            command => AddMessageIdParameter(command, messageId),
            cancellationToken);
    }

    public async Task MarkDuplicateAsync(
        string messageId,
        CancellationToken cancellationToken = default)
    {
        await ExecuteMarkAsync(
            SqlStoredProcedures.AuditIngestion.MarkDuplicate,
            command => AddMessageIdParameter(command, messageId),
            cancellationToken);
    }

    public async Task MarkIgnoredAsync(
        string messageId,
        CancellationToken cancellationToken = default)
    {
        await ExecuteMarkAsync(
            SqlStoredProcedures.AuditIngestion.MarkIgnored,
            command => AddMessageIdParameter(command, messageId),
            cancellationToken);
    }

    public async Task MarkFailedAsync(
        string messageId,
        AuditErrorInfo errorInfo,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(errorInfo);

        await ExecuteMarkAsync(
            SqlStoredProcedures.AuditIngestion.MarkFailed,
            command => AddFailureParameters(command, messageId, errorInfo),
            cancellationToken);
    }

    public async Task MarkDeadLetteredAsync(
        string messageId,
        AuditErrorInfo errorInfo,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(errorInfo);

        await ExecuteMarkAsync(
            SqlStoredProcedures.AuditIngestion.MarkDeadLettered,
            command => AddFailureParameters(command, messageId, errorInfo),
            cancellationToken);
    }

    public async Task<AuditIngestion?> GetByPublicIdAsync(
        string publicId,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteSingleAsync(
            SqlStoredProcedures.AuditIngestion.SelectByPublicId,
            command => command.Parameters.Add(
                SqlParameterFactory.Char(
                    SqlParameterNames.Common.PublicId,
                    publicId,
                    AuditConstants.PublicIdLength)),
            cancellationToken);
    }

    public async Task<AuditIngestion?> GetByMessageIdAsync(
        string messageId,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteSingleAsync(
            SqlStoredProcedures.AuditIngestion.SelectByMessageId,
            command => command.Parameters.Add(
                SqlParameterFactory.Char(
                    SqlParameterNames.Common.MessageId,
                    messageId,
                    AuditConstants.MessageIdLength)),
            cancellationToken);
    }

    public async Task<PagedQueryResult<AuditIngestion>> SearchAsync(
        AuditIngestionSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        int skip = (query.Page - 1) * query.PageSize;

        IReadOnlyList<AuditIngestion> items = await ExecuteListAsync(
            SqlStoredProcedures.AuditIngestion.SelectSkipAndTakeWhereDynamic,
            command => AddIngestionSearchParameters(command, query, skip),
            cancellationToken);

        int totalItems = await CountAsync(query, cancellationToken);

        return new PagedQueryResult<AuditIngestion>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalItems = totalItems
        };
    }

    public async Task<PagedQueryResult<AuditIngestion>> SearchFailedAsync(
        AuditFailedIngestionSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        int skip = (query.Page - 1) * query.PageSize;

        IReadOnlyList<AuditIngestion> items = await ExecuteListAsync(
            SqlStoredProcedures.AuditIngestion.SelectFailedWhereDynamic,
            command => AddFailedSearchParameters(command, query, skip),
            cancellationToken);

        int totalItems = await ExecuteScalarIntAsync(
            SqlStoredProcedures.AuditIngestion.GetFailedRecordCountWhereDynamic,
            command => AddFailedCountParameters(command, query),
            cancellationToken);

        return new PagedQueryResult<AuditIngestion>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalItems = totalItems
        };
    }

    public async Task<int> CountAsync(
        AuditIngestionSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await ExecuteScalarIntAsync(
            SqlStoredProcedures.AuditIngestion.GetRecordCountWhereDynamic,
            command => AddIngestionCountParameters(command, query),
            cancellationToken);
    }

    public async Task<int> CountFailedAsync(
        AuditDashboardSummarySearchQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await ExecuteScalarIntAsync(
            SqlStoredProcedures.AuditIngestion.CountFailedForDashboard,
            command => AddDashboardFilterParameters(command, query),
            cancellationToken);
    }

    public async Task<int> CountDuplicateAsync(
        AuditDashboardSummarySearchQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await ExecuteScalarIntAsync(
            SqlStoredProcedures.AuditIngestion.CountDuplicateForDashboard,
            command => AddDashboardFilterParameters(command, query),
            cancellationToken);
    }

    public async Task<int?> GetOldestFailedIngestionAgeSecondsAsync(
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteNullableScalarIntAsync(
            SqlStoredProcedures.AuditIngestion.GetOldestFailedIngestionAgeSeconds,
            command => command.Parameters.Add(
                SqlParameterFactory.DateTime2(
                    SqlParameterNames.Common.NowUtc,
                    nowUtc)),
            cancellationToken);
    }

    private static void AddUpsertProcessingParameters(
        SqlCommand command,
        AuditIngestion auditIngestion)
    {
        AddCommonUpsertParameters(
            command,
            publicId: auditIngestion.PublicId,
            messageId: auditIngestion.SourceEvent.MessageId,
            eventType: auditIngestion.SourceEvent.EventType,
            aggregateType: auditIngestion.AggregateRef.AggregateType,
            aggregateId: auditIngestion.AggregateRef.AggregateId,
            aggregatePublicId: auditIngestion.AggregateRef.AggregatePublicId,
            aggregateVersion: auditIngestion.AggregateRef.AggregateVersion,
            correlationId: auditIngestion.TraceContext.CorrelationId,
            sourcePriority: auditIngestion.SourceEvent.SourcePriority,
            sourceOccurredAtUtc: auditIngestion.SourceEvent.SourceOccurredAtUtc,
            sourcePublishedAtUtc: auditIngestion.SourceEvent.SourcePublishedAtUtc,
            consumerName: auditIngestion.ConsumerName);
    }

    private static void AddUpsertUnsupportedParameters(
        SqlCommand command,
        AuditUnsupportedIngestionUpsertCommand upsertCommand)
    {
        AddCommonUpsertParameters(
            command,
            publicId: upsertCommand.PublicId,
            messageId: upsertCommand.MessageId,
            eventType: upsertCommand.EventType,
            aggregateType: upsertCommand.AggregateType,
            aggregateId: upsertCommand.AggregateId,
            aggregatePublicId: upsertCommand.AggregatePublicId,
            aggregateVersion: upsertCommand.AggregateVersion,
            correlationId: upsertCommand.CorrelationId,
            sourcePriority: upsertCommand.Priority,
            sourceOccurredAtUtc: upsertCommand.OccurredAtUtc,
            sourcePublishedAtUtc: upsertCommand.PublishedAtUtc,
            consumerName: upsertCommand.ConsumerName);
    }

    private static void AddCommonUpsertParameters(
        SqlCommand command,
        string publicId,
        string messageId,
        string eventType,
        string? aggregateType,
        string? aggregateId,
        string? aggregatePublicId,
        int? aggregateVersion,
        string? correlationId,
        int? sourcePriority,
        DateTime sourceOccurredAtUtc,
        DateTime? sourcePublishedAtUtc,
        string consumerName)
    {
        command.Parameters.AddRange(
        [
            SqlParameterFactory.Char(SqlParameterNames.Common.PublicId, publicId, AuditConstants.PublicIdLength),
            SqlParameterFactory.Char(SqlParameterNames.Common.MessageId, messageId, AuditConstants.MessageIdLength),
            SqlParameterFactory.NVarChar(SqlParameterNames.Common.EventType, eventType, AuditConstants.MaxEventTypeLength),
            SqlParameterFactory.NVarChar(SqlParameterNames.Common.AggregateType, aggregateType, AuditConstants.MaxAggregateTypeLength),
            SqlParameterFactory.NVarChar(SqlParameterNames.Common.AggregateId, aggregateId, AuditConstants.MaxAggregateIdLength),
            SqlParameterFactory.Char(SqlParameterNames.Common.AggregatePublicId, aggregatePublicId, AuditConstants.PublicIdLength),
            SqlParameterFactory.Int(SqlParameterNames.Common.AggregateVersion, aggregateVersion),
            SqlParameterFactory.NVarChar(SqlParameterNames.Common.CorrelationId, correlationId, AuditConstants.MaxCorrelationIdLength),
            SqlParameterFactory.TinyInt(SqlParameterNames.Common.SourcePriority, sourcePriority),
            SqlParameterFactory.DateTime2(SqlParameterNames.Common.SourceOccurredAtUtc, sourceOccurredAtUtc),
            SqlParameterFactory.DateTime2(SqlParameterNames.Common.SourcePublishedAtUtc, sourcePublishedAtUtc),
            SqlParameterFactory.NVarChar(SqlParameterNames.AuditIngestion.ConsumerName, consumerName, AuditConstants.MaxConsumerNameLength),
            SqlParameterFactory.OutputBigInt(SqlParameterNames.AuditIngestion.AuditIngestionId),
            SqlParameterFactory.OutputBit(SqlParameterNames.AuditIngestion.WasInserted),
            SqlParameterFactory.OutputVarChar(SqlParameterNames.AuditIngestion.CurrentStatus, 30)
        ]);
    }

    private static void AddMessageIdParameter(
        SqlCommand command,
        string messageId)
    {
        command.Parameters.Add(
            SqlParameterFactory.Char(
                SqlParameterNames.Common.MessageId,
                messageId,
                AuditConstants.MessageIdLength));
    }

    private static void AddFailureParameters(
        SqlCommand command,
        string messageId,
        AuditErrorInfo errorInfo)
    {
        command.Parameters.AddRange(
        [
            SqlParameterFactory.Char(SqlParameterNames.Common.MessageId, messageId, AuditConstants.MessageIdLength),
            SqlParameterFactory.NVarChar(SqlParameterNames.AuditIngestion.LastErrorCode, errorInfo.LastErrorCode, AuditConstants.MaxErrorCodeLength),
            SqlParameterFactory.NVarChar(SqlParameterNames.AuditIngestion.LastErrorMessage, errorInfo.LastErrorMessage, AuditConstants.MaxErrorMessageLength),
            SqlParameterFactory.VarChar(SqlParameterNames.AuditIngestion.LastErrorClass, errorInfo.LastErrorClass, 30)
        ]);
    }

    private static void AddIngestionSearchParameters(
        SqlCommand command,
        AuditIngestionSearchQuery query,
        int skip)
    {
        AddIngestionCountParameters(command, query);
        command.Parameters.Add(SqlParameterFactory.NVarChar(SqlParameterNames.Common.SortBy, query.SortBy, 100));
        command.Parameters.Add(SqlParameterFactory.VarChar(SqlParameterNames.Common.SortDirection, query.SortDirection, 4));
        command.Parameters.Add(SqlParameterFactory.Int(SqlParameterNames.Common.Skip, skip));
        command.Parameters.Add(SqlParameterFactory.Int(SqlParameterNames.Common.Take, query.PageSize));
    }

    private static void AddIngestionCountParameters(
        SqlCommand command,
        AuditIngestionSearchQuery query)
    {
        command.Parameters.AddRange(
        [
            SqlParameterFactory.DateTime2(SqlParameterNames.Common.FromFirstReceivedAtUtc, query.FromUtc),
            SqlParameterFactory.DateTime2(SqlParameterNames.Common.ToFirstReceivedAtUtc, query.ToUtc),
            SqlParameterFactory.VarChar(SqlParameterNames.AuditIngestion.Status, query.Status, 30),
            SqlParameterFactory.Char(SqlParameterNames.Common.MessageId, query.MessageId, AuditConstants.MessageIdLength),
            SqlParameterFactory.NVarChar(SqlParameterNames.Common.EventType, query.EventType, AuditConstants.MaxEventTypeLength),
            SqlParameterFactory.NVarChar(SqlParameterNames.Common.AggregateType, query.AggregateType, AuditConstants.MaxAggregateTypeLength),
            SqlParameterFactory.NVarChar(SqlParameterNames.Common.AggregateId, query.AggregateId, AuditConstants.MaxAggregateIdLength),
            SqlParameterFactory.Char(SqlParameterNames.Common.AggregatePublicId, query.AggregatePublicId, AuditConstants.PublicIdLength),
            SqlParameterFactory.NVarChar(SqlParameterNames.Common.CorrelationId, query.CorrelationId, AuditConstants.MaxCorrelationIdLength),
            SqlParameterFactory.NVarChar(SqlParameterNames.AuditIngestion.ConsumerName, query.ConsumerName, AuditConstants.MaxConsumerNameLength),
            SqlParameterFactory.VarChar(SqlParameterNames.AuditIngestion.LastErrorClass, query.LastErrorClass, 30)
        ]);
    }

    private static void AddFailedSearchParameters(
        SqlCommand command,
        AuditFailedIngestionSearchQuery query,
        int skip)
    {
        AddFailedCountParameters(command, query);
        command.Parameters.Add(SqlParameterFactory.NVarChar(SqlParameterNames.Common.SortBy, query.SortBy, 100));
        command.Parameters.Add(SqlParameterFactory.VarChar(SqlParameterNames.Common.SortDirection, query.SortDirection, 4));
        command.Parameters.Add(SqlParameterFactory.Int(SqlParameterNames.Common.Skip, skip));
        command.Parameters.Add(SqlParameterFactory.Int(SqlParameterNames.Common.Take, query.PageSize));
    }

    private static void AddFailedCountParameters(
        SqlCommand command,
        AuditFailedIngestionSearchQuery query)
    {
        command.Parameters.AddRange(
        [
            SqlParameterFactory.DateTime2(SqlParameterNames.Common.FromFirstReceivedAtUtc, query.FromUtc),
            SqlParameterFactory.DateTime2(SqlParameterNames.Common.ToFirstReceivedAtUtc, query.ToUtc),
            SqlParameterFactory.NVarChar(SqlParameterNames.Common.EventType, query.EventType, AuditConstants.MaxEventTypeLength),
            SqlParameterFactory.NVarChar(SqlParameterNames.Common.AggregateType, query.AggregateType, AuditConstants.MaxAggregateTypeLength),
            SqlParameterFactory.NVarChar(SqlParameterNames.Common.AggregateId, query.AggregateId, AuditConstants.MaxAggregateIdLength),
            SqlParameterFactory.Char(SqlParameterNames.Common.AggregatePublicId, query.AggregatePublicId, AuditConstants.PublicIdLength),
            SqlParameterFactory.NVarChar(SqlParameterNames.Common.CorrelationId, query.CorrelationId, AuditConstants.MaxCorrelationIdLength),
            SqlParameterFactory.NVarChar(SqlParameterNames.AuditIngestion.ConsumerName, query.ConsumerName, AuditConstants.MaxConsumerNameLength),
            SqlParameterFactory.VarChar(SqlParameterNames.AuditIngestion.LastErrorClass, query.LastErrorClass, 30)
        ]);
    }

    private static void AddDashboardFilterParameters(
        SqlCommand command,
        AuditDashboardSummarySearchQuery query)
    {
        command.Parameters.AddRange(
        [
            SqlParameterFactory.DateTime2(SqlParameterNames.Common.FromFirstReceivedAtUtc, query.FromUtc),
            SqlParameterFactory.DateTime2(SqlParameterNames.Common.ToFirstReceivedAtUtc, query.ToUtc),
            SqlParameterFactory.NVarChar(SqlParameterNames.Common.SourceModule, query.SourceModule, AuditConstants.MaxSourceModuleLength)
        ]);
    }

    private async Task<AuditIngestionUpsertResult> ExecuteUpsertAsync(
        string storedProcedureName,
        Action<SqlCommand> configure,
        CancellationToken cancellationToken)
    {
        try
        {
            using SqlCommand command = CreateTransactionalStoredProcedureCommand(storedProcedureName);
            configure(command);

            await command.ExecuteNonQueryAsync(cancellationToken);

            return new AuditIngestionUpsertResult(
                AuditIngestionId: command.Parameters[SqlParameterNames.AuditIngestion.AuditIngestionId]
                    .GetRequiredInt64Value(),
                WasInserted: command.Parameters[SqlParameterNames.AuditIngestion.WasInserted]
                    .GetRequiredBooleanValue(),
                CurrentStatus: command.Parameters[SqlParameterNames.AuditIngestion.CurrentStatus]
                    .GetRequiredStringValue());
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    private async Task ExecuteMarkAsync(
        string storedProcedureName,
        Action<SqlCommand> configure,
        CancellationToken cancellationToken)
    {
        try
        {
            using SqlCommand command = CreateTransactionalStoredProcedureCommand(storedProcedureName);
            configure(command);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    private async Task<AuditIngestion?> ExecuteSingleAsync(
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
                    ? AuditIngestionDataMapper.Map(reader)
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

    private async Task<IReadOnlyList<AuditIngestion>> ExecuteListAsync(
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

                return await ReadAuditIngestionAsync(command, cancellationToken);
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

    private async Task<int?> ExecuteNullableScalarIntAsync(
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

                return SqlScalarValue.ToNullableInt32(scalar);
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

    private static async Task<IReadOnlyList<AuditIngestion>> ReadAuditIngestionAsync(
        SqlCommand command,
        CancellationToken cancellationToken)
    {
        List<AuditIngestion> ingestions = [];

        using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            ingestions.Add(AuditIngestionDataMapper.Map(reader));
        }

        return ingestions;
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
