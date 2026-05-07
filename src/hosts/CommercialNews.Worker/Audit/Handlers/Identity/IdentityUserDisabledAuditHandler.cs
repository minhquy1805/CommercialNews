using Audit.Application.Consumers.Identity;
using Audit.Application.Consumers.Identity.Payloads;
using Audit.Application.Contracts.Ingestion;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Outbox;

namespace CommercialNews.Worker.Audit.Handlers.Identity;

public sealed class IdentityUserDisabledAuditHandler
    : IdentityAuditIntegrationEventHandler<UserDisabledAuditPayload>
{
    public IdentityUserDisabledAuditHandler(
        IIdentityAuditEventIngestionService ingestionService,
        ILogger<IdentityUserDisabledAuditHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => IdentityIntegrationEventTypes.UserDisabled;

    protected override string EventDisplayName => "user disabled";

    protected override Task<Result<AuditIngestionResult>> IngestAsync(
        IdentityAuditEnvelopeContext context,
        UserDisabledAuditPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestUserDisabledAsync(
            context,
            payload,
            cancellationToken);
    }
}
