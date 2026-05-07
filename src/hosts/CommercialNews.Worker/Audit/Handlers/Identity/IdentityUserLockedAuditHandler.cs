using Audit.Application.Consumers.Identity;
using Audit.Application.Consumers.Identity.Payloads;
using Audit.Application.Contracts.Ingestion;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Outbox;

namespace CommercialNews.Worker.Audit.Handlers.Identity;

public sealed class IdentityUserLockedAuditHandler
    : IdentityAuditIntegrationEventHandler<UserLockedAuditPayload>
{
    public IdentityUserLockedAuditHandler(
        IIdentityAuditEventIngestionService ingestionService,
        ILogger<IdentityUserLockedAuditHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => IdentityIntegrationEventTypes.UserLocked;

    protected override string EventDisplayName => "user locked";

    protected override Task<Result<AuditIngestionResult>> IngestAsync(
        IdentityAuditEnvelopeContext context,
        UserLockedAuditPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestUserLockedAsync(
            context,
            payload,
            cancellationToken);
    }
}
