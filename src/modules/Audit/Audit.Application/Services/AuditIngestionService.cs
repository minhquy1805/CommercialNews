using Audit.Application.Contracts.Ingestion;
using Audit.Application.Errors;
using Audit.Application.Models;
using Audit.Application.Ports.Persistence;
using Audit.Domain.Entities;
using Audit.Domain.Exceptions;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Audit.Application.Services;

public sealed class AuditIngestionService : IAuditIngestionService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IAuditUnitOfWork _unitOfWork;

    public AuditIngestionService(
        IAuditLogRepository auditLogRepository,
        IAuditUnitOfWork unitOfWork)
    {
        _auditLogRepository = auditLogRepository
            ?? throw new ArgumentNullException(nameof(auditLogRepository));

        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<AuditIngestionResult>> IngestAsync(
        AuditIngestionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Error? validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return Result<AuditIngestionResult>.Failure(validationError);
        }

        try
        {
            AuditLog auditLog = AuditLog.Create(
                auditEventId: request.MessageId,
                actorUserId: request.ActorUserId,
                action: request.Action,
                resourceType: request.ResourceType,
                resourceId: request.ResourceId,
                outcome: request.Outcome,
                summary: request.Summary,
                reason: request.Reason,
                occurredAt: request.OccurredAtUtc,
                correlationId: request.CorrelationId,
                ipAddress: request.IpAddress,
                userAgent: request.UserAgent,
                oldValuesJson: request.OldValuesJson,
                newValuesJson: request.NewValuesJson,
                metadataJson: request.MetadataJson);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                AuditInsertResult insertResult =
                    await _auditLogRepository.InsertIfNotExistsAsync(
                        auditLog,
                        cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                AuditIngestionResult ingestionResult = insertResult.WasInserted
                    ? AuditIngestionResult.Inserted(insertResult.AuditId)
                    : AuditIngestionResult.Deduped(insertResult.AuditId);

                return Result<AuditIngestionResult>.Success(ingestionResult);
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (PersistenceException)
        {
            return Result<AuditIngestionResult>.Failure(
                AuditErrors.Ingestion.InsertFailed);
        }
        catch (AuditDomainException exception)
        {
            return Result<AuditIngestionResult>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error? ValidateRequest(AuditIngestionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.MessageId))
        {
            return AuditErrors.AuditLog.InvalidAuditEventId;
        }

        if (string.IsNullOrWhiteSpace(request.Action))
        {
            return AuditErrors.AuditLog.ActionRequired;
        }

        if (string.IsNullOrWhiteSpace(request.ResourceType))
        {
            return AuditErrors.AuditLog.ResourceTypeRequired;
        }

        if (string.IsNullOrWhiteSpace(request.ResourceId))
        {
            return AuditErrors.AuditLog.ResourceIdRequired;
        }

        if (string.IsNullOrWhiteSpace(request.Summary))
        {
            return AuditErrors.AuditLog.SummaryRequired;
        }

        if (request.OccurredAtUtc == default)
        {
            return AuditErrors.AuditLog.InvalidOccurredAt;
        }

        return null;
    }

    private static Error MapDomainException(AuditDomainException exception)
    {
        return exception.Code switch
        {
            "AUDIT.AUDIT_LOG_INVALID_EVENT_ID" =>
                AuditErrors.AuditLog.InvalidAuditEventId,

            "AUDIT.AUDIT_LOG_EVENT_ID_TOO_LONG" =>
                AuditErrors.AuditLog.InvalidAuditEventId,

            "AUDIT.AUDIT_LOG_INVALID_ACTOR_USER_ID" =>
                AuditErrors.AuditLog.InvalidActorUserId,

            "AUDIT.AUDIT_LOG_INVALID_ACTION" =>
                AuditErrors.AuditLog.ActionRequired,

            "AUDIT.AUDIT_LOG_ACTION_TOO_LONG" =>
                AuditErrors.AuditLog.ActionTooLong,

            "AUDIT.AUDIT_LOG_INVALID_RESOURCE_TYPE" =>
                AuditErrors.AuditLog.ResourceTypeRequired,

            "AUDIT.AUDIT_LOG_RESOURCE_TYPE_TOO_LONG" =>
                AuditErrors.AuditLog.ResourceTypeTooLong,

            "AUDIT.AUDIT_LOG_INVALID_RESOURCE_ID" =>
                AuditErrors.AuditLog.ResourceIdRequired,

            "AUDIT.AUDIT_LOG_RESOURCE_ID_TOO_LONG" =>
                AuditErrors.AuditLog.ResourceIdTooLong,

            "AUDIT.AUDIT_LOG_INVALID_OUTCOME" =>
                AuditErrors.AuditLog.InvalidOutcome,

            "AUDIT.AUDIT_LOG_INVALID_SUMMARY" =>
                AuditErrors.AuditLog.SummaryRequired,

            "AUDIT.AUDIT_LOG_SUMMARY_TOO_LONG" =>
                AuditErrors.AuditLog.SummaryTooLong,

            "AUDIT.AUDIT_LOG_REASON_TOO_LONG" =>
                AuditErrors.AuditLog.ReasonTooLong,

            "AUDIT.AUDIT_LOG_INVALID_OCCURRED_AT" =>
                AuditErrors.AuditLog.InvalidOccurredAt,

            "AUDIT.AUDIT_LOG_CORRELATION_ID_TOO_LONG" =>
                AuditErrors.AuditLog.CorrelationIdTooLong,

            "AUDIT.AUDIT_LOG_IP_ADDRESS_TOO_LONG" =>
                AuditErrors.AuditLog.IpAddressTooLong,

            "AUDIT.AUDIT_LOG_USER_AGENT_TOO_LONG" =>
                AuditErrors.AuditLog.UserAgentTooLong,

            _ => AuditErrors.ValidationFailed
        };
    }
}