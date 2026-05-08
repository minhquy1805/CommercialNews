using Audit.Application.Consumers.Identity;
using Audit.Application.Consumers.Identity.Payloads;
using Audit.Application.Contracts.Ingestion;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Outbox;

namespace CommercialNews.Worker.Audit.Handlers.Identity;

public sealed class IdentityUserSessionsRevokedAuditHandler
    : IdentityAuditIntegrationEventHandler<UserSessionsRevokedAuditPayload>
{
    public IdentityUserSessionsRevokedAuditHandler(
        IIdentityAuditEventIngestionService ingestionService,
        ILogger<IdentityUserSessionsRevokedAuditHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => IdentityIntegrationEventTypes.UserSessionsRevoked;

    protected override string EventDisplayName => "user sessions revoked";

    protected override Task<Result<AuditIngestionResult>> IngestAsync(
        IdentityAuditEnvelopeContext context,
        UserSessionsRevokedAuditPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestUserSessionsRevokedAsync(
            context,
            payload,
            cancellationToken);
    }
}
