using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.Counters.Requests;
using Interaction.Application.Contracts.Counters.Responses;
using Interaction.Application.Errors;
using Interaction.Application.Models.QueryModels;
using Interaction.Application.Ports.Persistence.Read;

namespace Interaction.Application.UseCases.GetArticleCounters;

public sealed class GetArticleCountersUseCase : IGetArticleCountersUseCase
{
    private readonly IArticleInteractionStatsQueryRepository _articleInteractionStatsQueryRepository;

    public GetArticleCountersUseCase(
        IArticleInteractionStatsQueryRepository articleInteractionStatsQueryRepository)
    {
        _articleInteractionStatsQueryRepository = articleInteractionStatsQueryRepository;
    }

    public async Task<Result<GetArticleCountersResponse>> ExecuteAsync(
        GetArticleCountersRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate the main input first.
        if (request.ArticleId <= 0)
        {
            return Result<GetArticleCountersResponse>.Failure(
                InteractionErrors.Article.InvalidArticleId);
        }

        // Read derived counters from the query side.
        // Counters are allowed to be missing or stale.
        ArticleCountersResult? result =
            await _articleInteractionStatsQueryRepository.GetCountersByArticleIdAsync(
                request.ArticleId,
                cancellationToken);

        // If no stats row exists yet, return a safe fallback.
        // This keeps the API stable without treating missing derived data as an error.
        if (result is null)
        {
            return Result<GetArticleCountersResponse>.Success(
                new GetArticleCountersResponse
                {
                    ArticleId = request.ArticleId,
                    Views = 0,
                    Likes = 0,
                    Comments = 0,
                    Partial = true
                });
        }

        var response = new GetArticleCountersResponse
        {
            ArticleId = result.ArticleId,
            Views = result.Views,
            Likes = result.Likes,
            Comments = result.Comments,
            Partial = result.Partial
        };

        return Result<GetArticleCountersResponse>.Success(response);
    }
}