using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;
using Content.Application.Errors;
using Content.Application.Ports.Persistence;
using Content.Domain.Entities;

namespace Content.Application.UseCases.ArticleLifecycleEvents.GetArticleLifecycleEvents;

public sealed class GetArticleLifecycleEventsUseCase : IGetArticleLifecycleEventsUseCase
{
    private readonly IArticleLifecycleEventRepository _articleLifecycleEventRepository;

    public GetArticleLifecycleEventsUseCase(
        IArticleLifecycleEventRepository articleLifecycleEventRepository)
    {
        _articleLifecycleEventRepository = articleLifecycleEventRepository
            ?? throw new ArgumentNullException(nameof(articleLifecycleEventRepository));
    }

    public async Task<Result<IReadOnlyList<ArticleLifecycleEventItemDto>>> ExecuteAsync(
        GetArticleLifecycleEventsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.ArticleId <= 0)
        {
            return Result<IReadOnlyList<ArticleLifecycleEventItemDto>>.Failure(
                ContentErrors.Article.InvalidArticleId);
        }

        IReadOnlyList<ArticleLifecycleEvent> lifecycleEvents =
            await _articleLifecycleEventRepository.GetByArticleIdAsync(
                request.ArticleId,
                cancellationToken);

        IReadOnlyList<ArticleLifecycleEventItemDto> response = lifecycleEvents
            .Select(static lifecycleEvent => new ArticleLifecycleEventItemDto
            {
                EventId = lifecycleEvent.EventId,
                ArticleId = lifecycleEvent.ArticleId,
                ArticleVersion = lifecycleEvent.ArticleVersion,
                ActionType = lifecycleEvent.ActionType,
                FromStatus = lifecycleEvent.FromStatus,
                ToStatus = lifecycleEvent.ToStatus,
                Reason = lifecycleEvent.Reason,
                ActorUserId = lifecycleEvent.ActorUserId,
                OccurredAt = lifecycleEvent.OccurredAt,
                CorrelationId = lifecycleEvent.CorrelationId,
                MetadataJson = lifecycleEvent.MetadataJson
            })
            .ToArray();

        return Result<IReadOnlyList<ArticleLifecycleEventItemDto>>.Success(response);
    }
}