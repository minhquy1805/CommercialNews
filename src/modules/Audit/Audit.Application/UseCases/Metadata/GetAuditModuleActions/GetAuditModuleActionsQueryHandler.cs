using Audit.Application.Errors;
using Audit.Application.Models.Queries.Metadata;
using Audit.Application.Models.Results.Metadata;
using Audit.Domain.Policies.Evidence;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using MediatR;

namespace Audit.Application.UseCases.Metadata.GetAuditModuleActions;

public sealed class GetAuditModuleActionsQueryHandler
    : IRequestHandler<GetAuditModuleActionsQuery, Result<AuditModuleActionsResult>>
{
    private readonly IAuditActionClassificationPolicy _actionClassificationPolicy;

    public GetAuditModuleActionsQueryHandler(
        IAuditActionClassificationPolicy actionClassificationPolicy)
    {
        _actionClassificationPolicy = actionClassificationPolicy
            ?? throw new ArgumentNullException(nameof(actionClassificationPolicy));
    }

    public Task<Result<AuditModuleActionsResult>> Handle(
        GetAuditModuleActionsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!AuditMetadataCatalog.TryGetCurrentModuleEventTypes(
                request.SourceModule,
                out var normalizedSourceModule,
                out var eventTypes))
        {
            return Task.FromResult(
                Result<AuditModuleActionsResult>.Failure(
                    AuditErrors.Validation.UnsupportedSourceModule));
        }

        var actions = eventTypes
            .Select(eventType =>
                _actionClassificationPolicy.Classify(
                    normalizedSourceModule,
                    eventType).Action)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Task.FromResult(
            Result<AuditModuleActionsResult>.Success(
                new AuditModuleActionsResult(
                    SourceModule: normalizedSourceModule,
                    Actions: actions)));
    }
}
