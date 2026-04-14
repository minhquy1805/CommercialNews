using Audit.Application.Contracts.Requests;
using Audit.Application.Contracts.Responses;
using Audit.Application.Errors;
using Audit.Application.Ports.Persistence;
using Audit.Domain.Exceptions;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.Results;

namespace Audit.Application.UseCases.GetAuditLogByEventId;

public sealed class GetAuditLogByEventIdUseCase : IGetAuditLogByEventIdUseCase
{
    private readonly IAuditLogRepository _auditLogRepository;

    public GetAuditLogByEventIdUseCase(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository
            ?? throw new ArgumentNullException(nameof(auditLogRepository));
    }

    public async Task<Result<GetAuditLogByEventIdResponse>> ExecuteAsync(
        GetAuditLogByEventIdRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.AuditEventId))
            {
                return Result<GetAuditLogByEventIdResponse>.Failure(
                    AuditErrors.AuditLog.InvalidAuditEventId);
            }

            if (request.AuditEventId.Trim().Length > 26)
            {
                return Result<GetAuditLogByEventIdResponse>.Failure(
                    AuditErrors.AuditLog.InvalidAuditEventId);
            }

            var auditLog = await _auditLogRepository.SelectDetailByAuditEventIdAsync(
                request.AuditEventId.Trim(),
                cancellationToken);

            if (auditLog is null)
            {
                return Result<GetAuditLogByEventIdResponse>.Failure(
                    AuditErrors.AuditLog.NotFound);
            }

            return Result<GetAuditLogByEventIdResponse>.Success(
                new GetAuditLogByEventIdResponse
                {
                    AuditId = auditLog.AuditId,
                    AuditEventId = auditLog.AuditEventId,
                    OccurredAt = auditLog.OccurredAt,
                    ActorUserId = auditLog.ActorUserId,
                    Action = auditLog.Action,
                    ResourceType = auditLog.ResourceType,
                    ResourceId = auditLog.ResourceId,
                    Outcome = auditLog.Outcome,
                    Summary = auditLog.Summary,
                    Reason = auditLog.Reason,
                    CorrelationId = auditLog.CorrelationId,
                    IpAddress = auditLog.IpAddress,
                    UserAgent = auditLog.UserAgent,
                    OldValuesJson = auditLog.OldValuesJson,
                    NewValuesJson = auditLog.NewValuesJson,
                    MetadataJson = auditLog.MetadataJson
                });
        }
        catch (PersistenceException exception)
        {
            return Result<GetAuditLogByEventIdResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (AuditDomainException exception)
        {
            return Result<GetAuditLogByEventIdResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error MapDomainException(AuditDomainException exception)
    {
        return exception.Code switch
        {
            "AUDIT.AUDIT_LOG_INVALID_EVENT_ID" => AuditErrors.AuditLog.InvalidAuditEventId,
            "AUDIT.AUDIT_LOG_EVENT_ID_TOO_LONG" => AuditErrors.AuditLog.InvalidAuditEventId,
            "AUDIT.AUDIT_LOG_INVALID_ACTOR_USER_ID" => AuditErrors.AuditLog.InvalidActorUserId,
            "AUDIT.AUDIT_LOG_INVALID_ACTION" => AuditErrors.AuditLog.ActionRequired,
            "AUDIT.AUDIT_LOG_ACTION_TOO_LONG" => AuditErrors.AuditLog.ActionTooLong,
            "AUDIT.AUDIT_LOG_INVALID_RESOURCE_TYPE" => AuditErrors.AuditLog.ResourceTypeRequired,
            "AUDIT.AUDIT_LOG_RESOURCE_TYPE_TOO_LONG" => AuditErrors.AuditLog.ResourceTypeTooLong,
            "AUDIT.AUDIT_LOG_INVALID_RESOURCE_ID" => AuditErrors.AuditLog.ResourceIdRequired,
            "AUDIT.AUDIT_LOG_RESOURCE_ID_TOO_LONG" => AuditErrors.AuditLog.ResourceIdTooLong,
            "AUDIT.AUDIT_LOG_INVALID_OUTCOME" => AuditErrors.AuditLog.InvalidOutcome,
            "AUDIT.AUDIT_LOG_INVALID_SUMMARY" => AuditErrors.AuditLog.SummaryRequired,
            "AUDIT.AUDIT_LOG_SUMMARY_TOO_LONG" => AuditErrors.AuditLog.SummaryTooLong,
            "AUDIT.AUDIT_LOG_REASON_TOO_LONG" => AuditErrors.AuditLog.ReasonTooLong,
            "AUDIT.AUDIT_LOG_INVALID_OCCURRED_AT" => AuditErrors.AuditLog.InvalidOccurredAt,
            "AUDIT.AUDIT_LOG_CORRELATION_ID_TOO_LONG" => AuditErrors.AuditLog.CorrelationIdTooLong,
            "AUDIT.AUDIT_LOG_IP_ADDRESS_TOO_LONG" => AuditErrors.AuditLog.IpAddressTooLong,
            "AUDIT.AUDIT_LOG_USER_AGENT_TOO_LONG" => AuditErrors.AuditLog.UserAgentTooLong,
            _ => AuditErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "AUDIT.LOG_NOT_FOUND" => AuditErrors.AuditLog.NotFound,
            "AUDIT.INVALID_AUDIT_EVENT_ID" => AuditErrors.AuditLog.InvalidAuditEventId,
            _ => AuditErrors.ValidationFailed
        };
    }
}