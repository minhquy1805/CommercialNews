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
    private const string PublishedStatus = "Published";

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

        string title = NormalizeTitle(
            payload.Title,
            payload.ArticlePublicId);

        string summary = NormalizeSummary(
            payload.Summary,
            title);

        string body = NormalizeBody(
            payload.Body,
            summary);

        var command = new ApplyContentArticleProjectionCommand(
            ArticleId: payload.ArticleId,
            ArticlePublicId: payload.ArticlePublicId,
            Title: title,
            Summary: summary,
            Body: body,
            CategoryId: payload.CategoryId > 0
                ? payload.CategoryId
                : null,
            CategoryName: NormalizeOptional(payload.CategoryName),
            AuthorUserId: payload.AuthorUserId > 0
                ? payload.AuthorUserId
                : null,
            AuthorDisplayName: null,
            CoverMediaId: payload.CoverMediaId,
            Tags: MapTags(payload.Tags),
            Status: payload.ToStatus,
            IsPublic: IsPublished(payload.ToStatus),
            PublishedAtUtc: payload.PublishedAtUtc,
            UpdatedAtUtc: payload.PublishedAtUtc,
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

        string title = NormalizeTitle(
            payload.Title,
            payload.ArticlePublicId);

        string summary = NormalizeSummary(
            payload.Summary,
            title);

        string body = NormalizeBody(
            payload.Body,
            summary);

        var command = new ApplyContentArticleProjectionCommand(
            ArticleId: payload.ArticleId,
            ArticlePublicId: payload.ArticlePublicId,
            Title: title,
            Summary: summary,
            Body: body,
            CategoryId: payload.CategoryId > 0
                ? payload.CategoryId
                : null,
            CategoryName: NormalizeOptional(payload.CategoryName),
            AuthorUserId: payload.AuthorUserId > 0
                ? payload.AuthorUserId
                : null,
            AuthorDisplayName: null,
            CoverMediaId: payload.CoverMediaId,
            Tags: MapTags(payload.Tags),
            Status: payload.Status,
            IsPublic: IsPublished(payload.Status),
            PublishedAtUtc: null,
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

    private static string NormalizeTitle(
        string? title,
        string articlePublicId)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            return title.Trim();
        }

        return articlePublicId.Trim();
    }

    private static string NormalizeSummary(
        string? summary,
        string fallbackTitle)
    {
        if (!string.IsNullOrWhiteSpace(summary))
        {
            return summary.Trim();
        }

        return fallbackTitle;
    }

    private static bool IsPublished(string? status)
    {
        return string.Equals(
            status?.Trim(),
            PublishedStatus,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeBody(
        string? body,
        string fallbackSummary)
    {
        if (!string.IsNullOrWhiteSpace(body))
        {
            return body.Trim();
        }

        return fallbackSummary;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static IReadOnlyCollection<ArticleTagProjectionItem>? MapTags(
        IReadOnlyCollection<ArticleTagReadingPayload>? tags)
    {
        if (tags is null)
        {
            return null;
        }

        return tags
            .Select(tag => new ArticleTagProjectionItem(
                TagId: tag.TagId,
                TagPublicId: tag.TagPublicId,
                Name: tag.Name,
                Slug: null))
            .ToArray();
    }
}
