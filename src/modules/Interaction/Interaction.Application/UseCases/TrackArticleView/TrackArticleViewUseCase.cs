using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Interaction.Application.Contracts.Views.Requests;
using Interaction.Application.Contracts.Views.Responses;
using Interaction.Application.Errors;
using Interaction.Application.Ports.Persistence.Transactions;
using Interaction.Application.Ports.Persistence.Write;
using Interaction.Domain.Entities;
using Interaction.Domain.Exceptions;

namespace Interaction.Application.UseCases.TrackArticleView;

public sealed class TrackArticleViewUseCase : ITrackArticleViewUseCase
{
    private readonly IArticleViewEventRepository _articleViewEventRepository;
    private readonly IInteractionUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public TrackArticleViewUseCase(
        IArticleViewEventRepository articleViewEventRepository,
        IInteractionUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _articleViewEventRepository = articleViewEventRepository
            ?? throw new ArgumentNullException(nameof(articleViewEventRepository));
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
        _dateTimeProvider = dateTimeProvider
            ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    public async Task<Result<TrackArticleViewResponse>> ExecuteAsync(
        TrackArticleViewRequest request,
        CancellationToken cancellationToken = default)
    {
        try
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
            ArticleViewEvent articleViewEvent = ArticleViewEvent.Create(
                articleId: request.ArticleId,
                userId: request.UserId,
                visitorKey: request.VisitorKey,
                ipAddress: request.IpAddress,
                userAgent: request.UserAgent,
                nowUtc: _dateTimeProvider.UtcNow);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // Persist the raw event only.
                // This use case does not update counters synchronously.
                await _articleViewEventRepository.InsertAsync(
                    articleViewEvent,
                    cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<TrackArticleViewResponse>.Success(
                    new TrackArticleViewResponse
                    {
                        Accepted = true
                    });
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (PersistenceException exception)
        {
            return Result<TrackArticleViewResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (InteractionDomainException exception)
        {
            return Result<TrackArticleViewResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error MapDomainException(InteractionDomainException exception)
    {
        return exception.Code switch
        {
            "INTERACTION.ARTICLE_VIEW_EVENT_INVALID_ARTICLE_ID" => InteractionErrors.Article.InvalidArticleId,
            "INTERACTION.ARTICLE_VIEW_EVENT_INVALID_USER_ID" => InteractionErrors.ValidationFailed,
            "INTERACTION.ARTICLE_VIEW_EVENT_VISITOR_KEY_TOO_LONG" => InteractionErrors.View.VisitorKeyTooLong,
            "INTERACTION.ARTICLE_VIEW_EVENT_IP_ADDRESS_TOO_LONG" => InteractionErrors.View.IpAddressTooLong,
            "INTERACTION.ARTICLE_VIEW_EVENT_USER_AGENT_TOO_LONG" => InteractionErrors.View.UserAgentTooLong,
            _ => InteractionErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "INTERACTION.ARTICLE_NOT_FOUND" => InteractionErrors.Article.NotFound,
            "INTERACTION.VIEW_INVALID_METADATA" => InteractionErrors.ValidationFailed,
            "INTERACTION.VALIDATION_FAILED" => InteractionErrors.ValidationFailed,
            _ => InteractionErrors.ValidationFailed
        };
    }
}