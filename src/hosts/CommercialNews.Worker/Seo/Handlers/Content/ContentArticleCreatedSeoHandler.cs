using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Outbox;
using Seo.Application.Consumers.Content;
using Seo.Application.Consumers.Content.Payloads;
using Seo.Application.Contracts.EventApply;

namespace CommercialNews.Worker.Seo.Handlers.Content;

public sealed class ContentArticleCreatedSeoHandler
    : ContentSeoIntegrationEventHandler<ArticleCreatedSeoPayload>
{
    public ContentArticleCreatedSeoHandler(
        IContentSeoEventApplyService applyService,
        ILogger<ContentArticleCreatedSeoHandler> logger)
        : base(applyService, logger)
    {
    }

    public override string EventType => ContentIntegrationEventTypes.ArticleCreated;

    protected override string EventDisplayName => "article created";

    protected override Task<Result<SeoEventApplyResult>> ApplyAsync(
        ContentSeoEnvelopeContext context,
        ArticleCreatedSeoPayload payload,
        CancellationToken cancellationToken)
    {
        return ApplyService.ApplyArticleCreatedAsync(
            context,
            payload,
            cancellationToken);
    }
}