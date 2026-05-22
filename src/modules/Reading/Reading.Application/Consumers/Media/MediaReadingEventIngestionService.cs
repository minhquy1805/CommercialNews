using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Consumers.Media.Payloads;
using Reading.Application.Models.Commands;
using Reading.Application.Models.Results;
using Reading.Application.UseCases.Projections.ApplyArticleMediaProjection;

namespace Reading.Application.Consumers.Media;

public sealed class MediaReadingEventIngestionService
    : IMediaReadingEventIngestionService
{
    private readonly IApplyArticleMediaProjectionUseCase _applyArticleMediaProjectionUseCase;

    public MediaReadingEventIngestionService(
        IApplyArticleMediaProjectionUseCase applyArticleMediaProjectionUseCase)
    {
        _applyArticleMediaProjectionUseCase = applyArticleMediaProjectionUseCase
            ?? throw new ArgumentNullException(nameof(applyArticleMediaProjectionUseCase));
    }

    public Task<Result<ArticleProjectionApplyResult>> IngestArticleMediaAttachedAsync(
        MediaReadingEnvelopeContext context,
        ArticleMediaAttachedReadingPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        var command = new UpsertArticleMediaProjectionCommand(
            ArticleId: payload.ArticleId,
            MediaId: payload.MediaId,
            MediaPublicId: payload.MediaPublicId,
            Url: payload.Url,
            Alt: GetEffectiveAlt(payload.EffectiveAltText, payload.AltTextOverride, payload.AltText),
            Caption: payload.Caption,
            MediaType: payload.MediaType,
            SortOrder: payload.SortOrder,
            IsPrimary: payload.IsPrimary,
            SourceVersion: payload.AttachmentSetVersion,
            MessageId: context.MessageId,
            SourceOccurredAtUtc: context.OccurredAtUtc);

        return _applyArticleMediaProjectionUseCase.UpsertAttachmentAsync(
            command,
            cancellationToken);
    }

    public Task<Result<ArticleProjectionApplyResult>> IngestArticlePrimaryMediaSetAsync(
        MediaReadingEnvelopeContext context,
        ArticlePrimaryMediaSetReadingPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        var command = new SetPrimaryArticleMediaProjectionCommand(
            ArticleId: payload.ArticleId,
            MediaId: payload.MediaId,
            MediaPublicId: payload.MediaPublicId,
            Url: payload.Url,
            Alt: GetEffectiveAlt(payload.EffectiveAltText, payload.AltTextOverride, payload.AltText),
            Caption: payload.Caption,
            MediaType: payload.MediaType,
            SortOrder: payload.SortOrder,
            SourceVersion: payload.AttachmentSetVersion,
            MessageId: context.MessageId,
            SourceOccurredAtUtc: context.OccurredAtUtc);

        return _applyArticleMediaProjectionUseCase.SetPrimaryAsync(
            command,
            cancellationToken);
    }

    public Task<Result<ArticleProjectionApplyResult>> IngestArticleMediaReorderedAsync(
        MediaReadingEnvelopeContext context,
        ArticleMediaReorderedReadingPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        var command = new ReorderArticleMediaProjectionCommand(
            ArticleId: payload.ArticleId,
            Items: payload.Items
                .Select(static item => new ArticleMediaProjectionOrderItem(
                    MediaId: item.MediaId,
                    SortOrder: item.SortOrder))
                .ToArray(),
            SourceVersion: payload.AttachmentSetVersion,
            MessageId: context.MessageId,
            SourceOccurredAtUtc: context.OccurredAtUtc);

        return _applyArticleMediaProjectionUseCase.ReorderAsync(
            command,
            cancellationToken);
    }

    public Task<Result<ArticleProjectionApplyResult>> IngestArticleMediaDetachedAsync(
        MediaReadingEnvelopeContext context,
        ArticleMediaDetachedReadingPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        var command = new DetachArticleMediaProjectionCommand(
            ArticleId: payload.ArticleId,
            MediaId: payload.MediaId,
            PrimaryCleared: payload.PrimaryCleared,
            SourceVersion: payload.AttachmentSetVersion,
            MessageId: context.MessageId,
            SourceOccurredAtUtc: context.OccurredAtUtc);

        return _applyArticleMediaProjectionUseCase.DetachAsync(
            command,
            cancellationToken);
    }

    private static string? GetEffectiveAlt(
        string? effectiveAltText,
        string? altTextOverride,
        string? altText)
    {
        if (!string.IsNullOrWhiteSpace(effectiveAltText))
        {
            return effectiveAltText.Trim();
        }

        if (!string.IsNullOrWhiteSpace(altTextOverride))
        {
            return altTextOverride.Trim();
        }

        return string.IsNullOrWhiteSpace(altText)
            ? null
            : altText.Trim();
    }
}
