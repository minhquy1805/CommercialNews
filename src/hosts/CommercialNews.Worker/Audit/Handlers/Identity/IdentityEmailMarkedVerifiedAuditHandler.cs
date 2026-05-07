using Audit.Application.Consumers.Identity;
using Audit.Application.Consumers.Identity.Payloads;
using Audit.Application.Contracts.Ingestion;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Outbox;

namespace CommercialNews.Worker.Audit.Handlers.Identity;

public sealed class IdentityEmailMarkedVerifiedAuditHandler
    : IdentityAuditIntegrationEventHandler<EmailMarkedVerifiedAuditPayload>
{
    public IdentityEmailMarkedVerifiedAuditHandler(
        IIdentityAuditEventIngestionService ingestionService,
        ILogger<IdentityEmailMarkedVerifiedAuditHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => IdentityIntegrationEventTypes.EmailMarkedVerified;

    protected override string EventDisplayName => "email marked verified";

    protected override Task<Result<AuditIngestionResult>> IngestAsync(
        IdentityAuditEnvelopeContext context,
        EmailMarkedVerifiedAuditPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestEmailMarkedVerifiedAsync(
            context,
            payload,
            cancellationToken);
    }
}
