using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;

namespace Content.Application.UseCases.ArticleLifecycleEvents.GetArticleLifecycleEvents;

public interface IGetArticleLifecycleEventsUseCase
{
    Task<Result<IReadOnlyList<ArticleLifecycleEventItemDto>>> ExecuteAsync(
        GetArticleLifecycleEventsRequestDto request,
        CancellationToken cancellationToken = default);
}