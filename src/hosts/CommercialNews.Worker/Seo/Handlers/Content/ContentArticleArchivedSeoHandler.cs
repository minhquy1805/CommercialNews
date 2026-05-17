using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Outbox;
using Seo.Application.Consumers.Content;
using Seo.Application.Consumers.Content.Payloads;
using Seo.Application.Contracts.EventApply;

namespace CommercialNews.Worker.Seo.Handlers.Content;

public sealed class ContentArticleArchivedSeoHandler
    : ContentSeoIntegrationEventHandler<ArticleArchivedSeoPayload>
{
    public ContentArticleArchivedSeoHandler(
        IContentSeoEventApplyService applyService,
        ILogger<ContentArticleArchivedSeoHandler> logger)
        : base(applyService, logger)
    {
    }

    public override string EventType => ContentIntegrationEventTypes.ArticleArchived;

    protected override string EventDisplayName => "article archived";

    protected override Task<Result<SeoEventApplyResult>> ApplyAsync(
        ContentSeoEnvelopeContext context,
        ArticleArchivedSeoPayload payload,
        CancellationToken cancellationToken)
    {
        return ApplyService.ApplyArticleArchivedAsync(
            context,
            payload,
            cancellationToken);
    }
}