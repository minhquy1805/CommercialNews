using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Outbox;
using Reading.Application.Consumers.Identity;
using Reading.Application.Consumers.Identity.Payloads;
using Reading.Application.Models.Results;

namespace CommercialNews.Worker.Reading.Handlers.Identity;

public sealed class IdentityUserRegisteredReadingHandler
    : IdentityReadingIntegrationEventHandler<UserRegisteredReadingPayload>
{
    public IdentityUserRegisteredReadingHandler(
        IIdentityReadingEventIngestionService ingestionService,
        ILogger<IdentityUserRegisteredReadingHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => IdentityIntegrationEventTypes.UserRegistered;

    protected override string EventDisplayName => "user registered";

    protected override Task<Result<ArticleProjectionApplyResult>> IngestAsync(
        IdentityReadingEnvelopeContext context,
        UserRegisteredReadingPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestUserRegisteredAsync(
            context,
            payload,
            cancellationToken);
    }
}
