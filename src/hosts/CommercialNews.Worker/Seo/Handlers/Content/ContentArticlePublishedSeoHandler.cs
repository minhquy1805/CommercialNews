using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Outbox;
using Seo.Application.Consumers.Content;
using Seo.Application.Consumers.Content.Payloads;
using Seo.Application.Contracts.EventApply;

namespace CommercialNews.Worker.Seo.Handlers.Content;

public sealed class ContentArticlePublishedSeoHandler
    : ContentSeoIntegrationEventHandler<ArticlePublishedSeoPayload>
{
    public ContentArticlePublishedSeoHandler(
        IContentSeoEventApplyService applyService,
        ILogger<ContentArticlePublishedSeoHandler> logger)
        : base(applyService, logger)
    {
    }

    public override string EventType => ContentIntegrationEventTypes.ArticlePublished;

    protected override string EventDisplayName => "article published";

    protected override Task<Result<SeoEventApplyResult>> ApplyAsync(
        ContentSeoEnvelopeContext context,
        ArticlePublishedSeoPayload payload,
        CancellationToken cancellationToken)
    {
        return ApplyService.ApplyArticlePublishedAsync(
            context,
            payload,
            cancellationToken);
    }
}