using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Consumers.Content.Payloads;
using Reading.Application.Models.Commands;
using Reading.Application.Models.Results;
using Reading.Application.UseCases.Projections.ApplyContentArticleProjection;
using Reading.Application.UseCases.Projections.MarkArticleProjectionNotPublic;

namespace Reading.Application.Consumers.Content;

public sealed class ContentReadingEventIngestionService
    : IContentReadingEventIngestionService
{
    private readonly IApplyContentArticleProjectionUseCase _applyContentArticleProjectionUseCase;
    private readonly IMarkArticleProjectionNotPublicUseCase _markArticleProjectionNotPublicUseCase;

    public ContentReadingEventIngestionService(
        IApplyContentArticleProjectionUseCase applyContentArticleProjectionUseCase,
        IMarkArticleProjectionNotPublicUseCase markArticleProjectionNotPublicUseCase)
    {
        _applyContentArticleProjectionUseCase =
            applyContentArticleProjectionUseCase
            ?? throw new ArgumentNullException(nameof(applyContentArticleProjectionUseCase));

        _markArticleProjectionNotPublicUseCase =
            markArticleProjectionNotPublicUseCase
            ?? throw new ArgumentNullException(nameof(markArticleProjectionNotPublicUseCase));
    }

    public Task<Result<ArticleProjectionApplyResult>> IngestArticlePublishedAsync(
        ContentReadingEnvelopeContext context,
        ArticlePublishedReadingPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        var command = new ApplyContentArticleProjectionCommand(
            ArticleId: payload.ArticleId,
            ArticlePublicId: payload.ArticlePublicId,
            Title: payload.Title,
            Summary: payload.Summary,
            Body: payload.Body,
            CategoryId: payload.CategoryId,
            CategoryName: payload.CategoryName,
            AuthorUserId: payload.AuthorUserId,
            AuthorDisplayName: payload.AuthorDisplayName,
            Status: payload.Status,
            IsPublic: payload.IsPublic,
            PublishedAtUtc: payload.PublishedAtUtc,
            UpdatedAtUtc: payload.UpdatedAtUtc,
            SourceVersion: payload.Version,
            MessageId: context.MessageId,
            SourceOccurredAtUtc: context.OccurredAtUtc);

        return _applyContentArticleProjectionUseCase.ExecuteAsync(
            command,
            cancellationToken);
    }

    public Task<Result<ArticleProjectionApplyResult>> IngestArticleUpdatedAsync(
        ContentReadingEnvelopeContext context,
        ArticleUpdatedReadingPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        var command = new ApplyContentArticleProjectionCommand(
            ArticleId: payload.ArticleId,
            ArticlePublicId: payload.ArticlePublicId,
            Title: payload.Title,
            Summary: payload.Summary,
            Body: payload.Body,
            CategoryId: payload.CategoryId,
            CategoryName: payload.CategoryName,
            AuthorUserId: payload.AuthorUserId,
            AuthorDisplayName: payload.AuthorDisplayName,
            Status: payload.Status,
            IsPublic: payload.IsPublic,
            PublishedAtUtc: payload.PublishedAtUtc,
            UpdatedAtUtc: payload.UpdatedAtUtc,
            SourceVersion: payload.Version,
            MessageId: context.MessageId,
            SourceOccurredAtUtc: context.OccurredAtUtc);

        return _applyContentArticleProjectionUseCase.ExecuteAsync(
            command,
            cancellationToken);
    }

    public Task<Result<ArticleProjectionApplyResult>> IngestArticleUnpublishedAsync(
        ContentReadingEnvelopeContext context,
        ArticleUnpublishedReadingPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        var command = new MarkArticleProjectionNotPublicCommand(
            ArticleId: payload.ArticleId,
            Status: payload.ToStatus,
            SourceVersion: payload.Version,
            MessageId: context.MessageId,
            SourceOccurredAtUtc: context.OccurredAtUtc);

        return _markArticleProjectionNotPublicUseCase.ExecuteAsync(
            command,
            cancellationToken);
    }

    public Task<Result<ArticleProjectionApplyResult>> IngestArticleArchivedAsync(
        ContentReadingEnvelopeContext context,
        ArticleArchivedReadingPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        var command = new MarkArticleProjectionNotPublicCommand(
            ArticleId: payload.ArticleId,
            Status: payload.ToStatus,
            SourceVersion: payload.Version,
            MessageId: context.MessageId,
            SourceOccurredAtUtc: context.OccurredAtUtc);

        return _markArticleProjectionNotPublicUseCase.ExecuteAsync(
            command,
            cancellationToken);
    }

    public Task<Result<ArticleProjectionApplyResult>> IngestArticleSoftDeletedAsync(
        ContentReadingEnvelopeContext context,
        ArticleSoftDeletedReadingPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        var command = new MarkArticleProjectionNotPublicCommand(
            ArticleId: payload.ArticleId,
            Status: payload.ToStatus,
            SourceVersion: payload.Version,
            MessageId: context.MessageId,
            SourceOccurredAtUtc: context.OccurredAtUtc);

        return _markArticleProjectionNotPublicUseCase.ExecuteAsync(
            command,
            cancellationToken);
    }
}