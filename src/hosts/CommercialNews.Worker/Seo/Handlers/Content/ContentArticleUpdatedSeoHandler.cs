using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Outbox;
using Seo.Application.Consumers.Content;
using Seo.Application.Consumers.Content.Payloads;
using Seo.Application.Contracts.EventApply;

namespace CommercialNews.Worker.Seo.Handlers.Content;

public sealed class ContentArticleUpdatedSeoHandler
    : ContentSeoIntegrationEventHandler<ArticleUpdatedSeoPayload>
{
    public ContentArticleUpdatedSeoHandler(
        IContentSeoEventApplyService applyService,
        ILogger<ContentArticleUpdatedSeoHandler> logger)
        : base(applyService, logger)
    {
    }

    public override string EventType => ContentIntegrationEventTypes.ArticleUpdated;

    protected override string EventDisplayName => "article updated";

    protected override Task<Result<SeoEventApplyResult>> ApplyAsync(
        ContentSeoEnvelopeContext context,
        ArticleUpdatedSeoPayload payload,
        CancellationToken cancellationToken)
    {
        return ApplyService.ApplyArticleUpdatedAsync(
            context,
            payload,
            cancellationToken);
    }
}