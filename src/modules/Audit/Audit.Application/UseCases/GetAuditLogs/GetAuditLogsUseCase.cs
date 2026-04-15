using Audit.Application.Contracts.Requests;
using Audit.Application.Contracts.Responses;
using Audit.Application.Errors;
using Audit.Application.Models.QueryModels;
using Audit.Application.Ports.Persistence;
using Audit.Domain.Enums;
using Audit.Domain.Exceptions;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Audit.Application.UseCases.GetAuditLogs;

public sealed class GetAuditLogsUseCase : IGetAuditLogsUseCase
{
    private const int MaxPageSize = 200;

    private readonly IAuditLogRepository _auditLogRepository;

    public GetAuditLogsUseCase(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository
            ?? throw new ArgumentNullException(nameof(auditLogRepository));
    }

    public async Task<Result<GetAuditLogsResponse>> ExecuteAsync(
        GetAuditLogsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.Page <= 0)
            {
                return Result<GetAuditLogsResponse>.Failure(
                    AuditErrors.Query.InvalidPage);
            }

            if (request.PageSize <= 0)
            {
                return Result<GetAuditLogsResponse>.Failure(
                    AuditErrors.Query.InvalidPageSize);
            }

            if (request.PageSize > MaxPageSize)
            {
                return Result<GetAuditLogsResponse>.Failure(
                    AuditErrors.Query.InvalidPageSize);
            }

            if (request.FromOccurredAt.HasValue
                && request.ToOccurredAt.HasValue
                && request.FromOccurredAt.Value > request.ToOccurredAt.Value)
            {
                return Result<GetAuditLogsResponse>.Failure(
                    AuditErrors.Query.InvalidTimeRange);
            }

            if (request.ActorUserId.HasValue && request.ActorUserId.Value <= 0)
            {
                return Result<GetAuditLogsResponse>.Failure(
                    AuditErrors.AuditLog.InvalidActorUserId);
            }

            if (!string.IsNullOrWhiteSpace(request.AuditEventId)
                && request.AuditEventId.Trim().Length > 26)
            {
                return Result<GetAuditLogsResponse>.Failure(
                    AuditErrors.AuditLog.InvalidAuditEventId);
            }

            if (!string.IsNullOrWhiteSpace(request.Outcome)
                && !AuditOutcome.IsValid(request.Outcome))
            {
                return Result<GetAuditLogsResponse>.Failure(
                    AuditErrors.AuditLog.InvalidOutcome);
            }

            int skip = (request.Page - 1) * request.PageSize;

            AuditLogListQuery query = new()
            {
                FromOccurredAt = request.FromOccurredAt,
                ToOccurredAt = request.ToOccurredAt,
                ActorUserId = request.ActorUserId,
                Action = NormalizeOptional(request.Action),
                ResourceType = NormalizeOptional(request.ResourceType),
                ResourceId = NormalizeOptional(request.ResourceId),
                CorrelationId = NormalizeOptional(request.CorrelationId),
                AuditEventId = NormalizeOptional(request.AuditEventId),
                Outcome = NormalizeOptional(request.Outcome),
                Skip = skip,
                Take = request.PageSize
            };

            var queryResult = await _auditLogRepository.SelectSkipAndTakeAsync(
                query,
                cancellationToken);

            IReadOnlyList<AuditLogListItemResponse> items = queryResult.Items
                .Select(MapItem)
                .ToArray();

            int totalItems = queryResult.TotalItems;
            int totalPages = totalItems == 0
                ? 0
                : (int)Math.Ceiling(totalItems / (double)request.PageSize);

            return Result<GetAuditLogsResponse>.Success(
                new GetAuditLogsResponse
                {
                    Items = items,
                    Page = queryResult.Page,
                    PageSize = queryResult.PageSize,
                    TotalCount = totalItems,
                    TotalPages = totalPages
                });
        }
        catch (PersistenceException exception)
        {
            return Result<GetAuditLogsResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (AuditDomainException exception)
        {
            return Result<GetAuditLogsResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private static AuditLogListItemResponse MapItem(AuditLogListResultItem source)
    {
        return new AuditLogListItemResponse
        {
            AuditId = source.AuditId,
            AuditEventId = source.AuditEventId,
            OccurredAt = source.OccurredAt,
            ActorUserId = source.ActorUserId,
            Action = source.Action,
            ResourceType = source.ResourceType,
            ResourceId = source.ResourceId,
            Outcome = source.Outcome,
            Summary = source.Summary,
            CorrelationId = source.CorrelationId
        };
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
            "AUDIT.INVALID_TIME_RANGE" => AuditErrors.Query.InvalidTimeRange,
            "AUDIT.INVALID_PAGE" => AuditErrors.Query.InvalidPage,
            "AUDIT.INVALID_PAGE_SIZE" => AuditErrors.Query.InvalidPageSize,
            "AUDIT.INVALID_AUDIT_EVENT_ID" => AuditErrors.AuditLog.InvalidAuditEventId,
            "AUDIT.INVALID_ACTOR_USER_ID" => AuditErrors.AuditLog.InvalidActorUserId,
            _ => AuditErrors.ValidationFailed
        };
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}