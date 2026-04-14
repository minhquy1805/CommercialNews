using Audit.Application.Contracts.Requests;
using Audit.Application.Contracts.Responses;
using Audit.Application.Errors;
using Audit.Application.Models.QueryModels;
using Audit.Application.Ports.Persistence;
using Audit.Domain.Exceptions;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.Results;

namespace Audit.Application.UseCases.GetAuditLogsByCorrelationId;

public sealed class GetAuditLogsByCorrelationIdUseCase : IGetAuditLogsByCorrelationIdUseCase
{
    private const int MaxPageSize = 200;

    private readonly IAuditLogRepository _auditLogRepository;

    public GetAuditLogsByCorrelationIdUseCase(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository
            ?? throw new ArgumentNullException(nameof(auditLogRepository));
    }

    public async Task<Result<GetAuditLogsByCorrelationIdResponse>> ExecuteAsync(
        GetAuditLogsByCorrelationIdRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.CorrelationId))
            {
                return Result<GetAuditLogsByCorrelationIdResponse>.Failure(
                    AuditErrors.Query.CorrelationIdRequired);
            }

            if (request.Page <= 0)
            {
                return Result<GetAuditLogsByCorrelationIdResponse>.Failure(
                    AuditErrors.Query.InvalidPage);
            }

            if (request.PageSize <= 0)
            {
                return Result<GetAuditLogsByCorrelationIdResponse>.Failure(
                    AuditErrors.Query.InvalidPageSize);
            }

            if (request.PageSize > MaxPageSize)
            {
                return Result<GetAuditLogsByCorrelationIdResponse>.Failure(
                    AuditErrors.Query.InvalidPageSize);
            }

            int skip = (request.Page - 1) * request.PageSize;

            AuditLogByCorrelationQuery query = new()
            {
                CorrelationId = request.CorrelationId.Trim(),
                Skip = skip,
                Take = request.PageSize
            };

            var queryResult = await _auditLogRepository.SelectByCorrelationIdAsync(
                query,
                cancellationToken);

            IReadOnlyList<AuditLogListItemResponse> items = queryResult.Items
                .Select(MapItem)
                .ToArray();

            int totalItems = queryResult.TotalItems;
            int totalPages = totalItems == 0
                ? 0
                : (int)Math.Ceiling(totalItems / (double)request.PageSize);

            return Result<GetAuditLogsByCorrelationIdResponse>.Success(
                new GetAuditLogsByCorrelationIdResponse
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
            return Result<GetAuditLogsByCorrelationIdResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (AuditDomainException exception)
        {
            return Result<GetAuditLogsByCorrelationIdResponse>.Failure(
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
            "AUDIT.AUDIT_LOG_CORRELATION_ID_TOO_LONG" => AuditErrors.AuditLog.CorrelationIdTooLong,
            _ => AuditErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "AUDIT.CORRELATION_ID_REQUIRED" => AuditErrors.Query.CorrelationIdRequired,
            "AUDIT.INVALID_PAGE" => AuditErrors.Query.InvalidPage,
            "AUDIT.INVALID_PAGE_SIZE" => AuditErrors.Query.InvalidPageSize,
            _ => AuditErrors.ValidationFailed
        };
    }
}