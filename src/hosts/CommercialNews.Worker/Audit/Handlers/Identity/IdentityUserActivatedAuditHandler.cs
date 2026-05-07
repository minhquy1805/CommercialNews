using Audit.Application.Consumers.Identity;
using Audit.Application.Consumers.Identity.Payloads;
using Audit.Application.Contracts.Ingestion;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Outbox;

namespace CommercialNews.Worker.Audit.Handlers.Identity;

public sealed class IdentityUserActivatedAuditHandler
    : IdentityAuditIntegrationEventHandler<UserActivatedAuditPayload>
{
    public IdentityUserActivatedAuditHandler(
        IIdentityAuditEventIngestionService ingestionService,
        ILogger<IdentityUserActivatedAuditHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => IdentityIntegrationEventTypes.UserActivated;

    protected override string EventDisplayName => "user activated";

    protected override Task<Result<AuditIngestionResult>> IngestAsync(
        IdentityAuditEnvelopeContext context,
        UserActivatedAuditPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestUserActivatedAsync(
            context,
            payload,
            cancellationToken);
    }
}
