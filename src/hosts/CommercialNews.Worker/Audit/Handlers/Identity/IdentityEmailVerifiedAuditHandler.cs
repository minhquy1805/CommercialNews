using Audit.Application.Consumers.Identity;
using Audit.Application.Consumers.Identity.Payloads;
using Audit.Application.Contracts.Ingestion;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Outbox;

namespace CommercialNews.Worker.Audit.Handlers.Identity;

public sealed class IdentityEmailVerifiedAuditHandler
    : IdentityAuditIntegrationEventHandler<EmailVerifiedAuditPayload>
{
    public IdentityEmailVerifiedAuditHandler(
        IIdentityAuditEventIngestionService ingestionService,
        ILogger<IdentityEmailVerifiedAuditHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => IdentityIntegrationEventTypes.EmailVerified;

    protected override string EventDisplayName => "email verified";

    protected override Task<Result<AuditIngestionResult>> IngestAsync(
        IdentityAuditEnvelopeContext context,
        EmailVerifiedAuditPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestEmailVerifiedAsync(
            context,
            payload,
            cancellationToken);
    }
}
