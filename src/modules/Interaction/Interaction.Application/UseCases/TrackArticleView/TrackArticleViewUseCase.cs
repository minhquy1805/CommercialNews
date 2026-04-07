using CommercialNews.BuildingBlocks.Abstractions.Time;
using CommercialNews.BuildingBlocks.Results;
using Interaction.Application.Contracts.Views.Requests;
using Interaction.Application.Contracts.Views.Responses;
using Interaction.Application.Errors;
using Interaction.Application.Ports.Persistence.Write;
using Interaction.Domain.Entities;

namespace Interaction.Application.UseCases.TrackArticleView;

public sealed class TrackArticleViewUseCase : ITrackArticleViewUseCase
{
    private readonly IArticleViewEventRepository _articleViewEventRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public TrackArticleViewUseCase(
        IArticleViewEventRepository articleViewEventRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _articleViewEventRepository = articleViewEventRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<TrackArticleViewResponse>> ExecuteAsync(
        TrackArticleViewRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate the main input first.
        if (request.ArticleId <= 0)
        {
            return Result<TrackArticleViewResponse>.Failure(
                InteractionErrors.Article.InvalidArticleId);
        }

        // UserId is optional, but if provided it must be valid.
        if (request.UserId.HasValue && request.UserId.Value <= 0)
        {
            return Result<TrackArticleViewResponse>.Failure(
                InteractionErrors.ValidationFailed);
        }

        // VisitorKey is optional metadata for future dedupe/throttling policies.
        if (!string.IsNullOrWhiteSpace(request.VisitorKey) && request.VisitorKey.Trim().Length > 100)
        {
            return Result<TrackArticleViewResponse>.Failure(
                InteractionErrors.View.VisitorKeyTooLong);
        }

        // IP address is optional metadata.
        if (!string.IsNullOrWhiteSpace(request.IpAddress) && request.IpAddress.Trim().Length > 64)
        {
            return Result<TrackArticleViewResponse>.Failure(
                InteractionErrors.View.IpAddressTooLong);
        }

        // UserAgent is optional metadata.
        if (!string.IsNullOrWhiteSpace(request.UserAgent) && request.UserAgent.Trim().Length > 512)
        {
            return Result<TrackArticleViewResponse>.Failure(
                InteractionErrors.View.UserAgentTooLong);
        }

        // Create the raw view event.
        var articleViewEvent = ArticleViewEvent.Create(
            articleId: request.ArticleId,
            userId: request.UserId,
            visitorKey: request.VisitorKey,
            ipAddress: request.IpAddress,
            userAgent: request.UserAgent,
            nowUtc: _dateTimeProvider.UtcNow);

        // Persist the raw event only.
        // This use case does not update counters synchronously.
        await _articleViewEventRepository.InsertAsync(
            articleViewEvent,
            cancellationToken);

        // Return accepted result.
        return Result<TrackArticleViewResponse>.Success(
            new TrackArticleViewResponse
            {
                Accepted = true
            });
    }
}