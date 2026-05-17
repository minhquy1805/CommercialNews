using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Outbox;
using Seo.Application.Consumers.Content;
using Seo.Application.Consumers.Content.Payloads;
using Seo.Application.Contracts.EventApply;

namespace CommercialNews.Worker.Seo.Handlers.Content;

public sealed class ContentArticleUnpublishedSeoHandler
    : ContentSeoIntegrationEventHandler<ArticleUnpublishedSeoPayload>
{
    public ContentArticleUnpublishedSeoHandler(
        IContentSeoEventApplyService applyService,
        ILogger<ContentArticleUnpublishedSeoHandler> logger)
        : base(applyService, logger)
    {
    }

    public override string EventType => ContentIntegrationEventTypes.ArticleUnpublished;

    protected override string EventDisplayName => "article unpublished";

    protected override Task<Result<SeoEventApplyResult>> ApplyAsync(
        ContentSeoEnvelopeContext context,
        ArticleUnpublishedSeoPayload payload,
        CancellationToken cancellationToken)
    {
        return ApplyService.ApplyArticleUnpublishedAsync(
            context,
            payload,
            cancellationToken);
    }
}