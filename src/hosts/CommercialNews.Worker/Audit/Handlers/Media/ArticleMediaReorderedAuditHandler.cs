using Audit.Application.Consumers.Media;
using Audit.Application.Consumers.Media.Payloads;
using Audit.Application.Contracts.Ingestion;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Outbox;

namespace CommercialNews.Worker.Audit.Handlers.Media;

public sealed class ArticleMediaReorderedAuditHandler
    : MediaAuditIntegrationEventHandler<ArticleMediaReorderedAuditPayload>
{
    public ArticleMediaReorderedAuditHandler(
        IMediaAuditEventIngestionService ingestionService,
        ILogger<ArticleMediaReorderedAuditHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => MediaIntegrationEventTypes.ArticleMediaReordered;

    protected override string EventDisplayName => "article media reordered";

    protected override Task<Result<AuditIngestionResult>> IngestAsync(
        MediaAuditEnvelopeContext context,
        ArticleMediaReorderedAuditPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestArticleMediaReorderedAsync(
            context,
            payload,
            cancellationToken);
    }
}