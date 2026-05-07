using Audit.Application.Consumers.Identity;
using Audit.Application.Consumers.Identity.Payloads;
using Audit.Application.Contracts.Ingestion;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Outbox;

namespace CommercialNews.Worker.Audit.Handlers.Identity;

public sealed class IdentityPasswordChangedAuditHandler
    : IdentityAuditIntegrationEventHandler<PasswordChangedAuditPayload>
{
    public IdentityPasswordChangedAuditHandler(
        IIdentityAuditEventIngestionService ingestionService,
        ILogger<IdentityPasswordChangedAuditHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => IdentityIntegrationEventTypes.PasswordChanged;

    protected override string EventDisplayName => "password changed";

    protected override Task<Result<AuditIngestionResult>> IngestAsync(
        IdentityAuditEnvelopeContext context,
        PasswordChangedAuditPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestPasswordChangedAsync(
            context,
            payload,
            cancellationToken);
    }
}
