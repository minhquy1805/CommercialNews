using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Outbox;
using Reading.Application.Consumers.Identity;
using Reading.Application.Consumers.Identity.Payloads;
using Reading.Application.Models.Results;

namespace CommercialNews.Worker.Reading.Handlers.Identity;

public sealed class IdentityUserPublicProfileUpdatedReadingHandler
    : IdentityReadingIntegrationEventHandler<UserPublicProfileUpdatedReadingPayload>
{
    public IdentityUserPublicProfileUpdatedReadingHandler(
        IIdentityReadingEventIngestionService ingestionService,
        ILogger<IdentityUserPublicProfileUpdatedReadingHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType =>
        IdentityIntegrationEventTypes.UserPublicProfileUpdated;

    protected override string EventDisplayName => "user public profile updated";

    protected override Task<Result<ArticleProjectionApplyResult>> IngestAsync(
        IdentityReadingEnvelopeContext context,
        UserPublicProfileUpdatedReadingPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestUserPublicProfileUpdatedAsync(
            context,
            payload,
            cancellationToken);
    }
}
