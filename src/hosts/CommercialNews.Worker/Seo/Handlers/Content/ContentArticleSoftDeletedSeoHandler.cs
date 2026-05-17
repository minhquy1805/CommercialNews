using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Outbox;
using Seo.Application.Consumers.Content;
using Seo.Application.Consumers.Content.Payloads;
using Seo.Application.Contracts.EventApply;

namespace CommercialNews.Worker.Seo.Handlers.Content;

public sealed class ContentArticleSoftDeletedSeoHandler
    : ContentSeoIntegrationEventHandler<ArticleSoftDeletedSeoPayload>
{
    public ContentArticleSoftDeletedSeoHandler(
        IContentSeoEventApplyService applyService,
        ILogger<ContentArticleSoftDeletedSeoHandler> logger)
        : base(applyService, logger)
    {
    }

    public override string EventType => ContentIntegrationEventTypes.ArticleSoftDeleted;

    protected override string EventDisplayName => "article soft-deleted";

    protected override Task<Result<SeoEventApplyResult>> ApplyAsync(
        ContentSeoEnvelopeContext context,
        ArticleSoftDeletedSeoPayload payload,
        CancellationToken cancellationToken)
    {
        return ApplyService.ApplyArticleSoftDeletedAsync(
            context,
            payload,
            cancellationToken);
    }
}