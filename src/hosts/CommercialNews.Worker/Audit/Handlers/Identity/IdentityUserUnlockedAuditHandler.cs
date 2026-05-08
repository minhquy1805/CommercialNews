using Audit.Application.Consumers.Identity;
using Audit.Application.Consumers.Identity.Payloads;
using Audit.Application.Contracts.Ingestion;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Outbox;

namespace CommercialNews.Worker.Audit.Handlers.Identity;

public sealed class IdentityUserUnlockedAuditHandler
    : IdentityAuditIntegrationEventHandler<UserUnlockedAuditPayload>
{
    public IdentityUserUnlockedAuditHandler(
        IIdentityAuditEventIngestionService ingestionService,
        ILogger<IdentityUserUnlockedAuditHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => IdentityIntegrationEventTypes.UserUnlocked;

    protected override string EventDisplayName => "user unlocked";

    protected override Task<Result<AuditIngestionResult>> IngestAsync(
        IdentityAuditEnvelopeContext context,
        UserUnlockedAuditPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestUserUnlockedAsync(
            context,
            payload,
            cancellationToken);
    }
}
