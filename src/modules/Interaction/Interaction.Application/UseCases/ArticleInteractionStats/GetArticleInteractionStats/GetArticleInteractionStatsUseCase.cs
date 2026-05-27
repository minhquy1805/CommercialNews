using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.ArticleInteractionStats.GetArticleInteractionStats;
using Interaction.Application.Errors;
using Interaction.Application.Ports.Persistence;
using Interaction.Application.Validation.ArticleInteractionStats;
using ArticleInteractionStatsEntity = Interaction.Domain.Entities.ArticleInteractionStats;

namespace Interaction.Application.UseCases.ArticleInteractionStats.GetArticleInteractionStats;

public sealed class GetArticleInteractionStatsUseCase
    : IGetArticleInteractionStatsUseCase
{
    private readonly IArticleInteractionStatsRepository _statsRepository;

    public GetArticleInteractionStatsUseCase(
        IArticleInteractionStatsRepository statsRepository)
    {
        _statsRepository = statsRepository
            ?? throw new ArgumentNullException(nameof(statsRepository));
    }

    public async Task<Result<GetArticleInteractionStatsResponseDto>> ExecuteAsync(
        GetArticleInteractionStatsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationError =
            GetArticleInteractionStatsValidator.Validate(request);

        if (validationError is not null)
        {
            return Result<GetArticleInteractionStatsResponseDto>.Failure(
                validationError);
        }

        var articlePublicId =
            GetArticleInteractionStatsValidator.NormalizeArticlePublicId(
                request.ArticlePublicId);

        var stats = await _statsRepository.GetByArticlePublicIdAsync(
            articlePublicId,
            cancellationToken);

        if (stats is null)
        {
            return Result<GetArticleInteractionStatsResponseDto>.Failure(
                InteractionErrors.Counter.StatsNotFound);
        }

        return Result<GetArticleInteractionStatsResponseDto>.Success(
            MapResponse(stats));
    }

    private static GetArticleInteractionStatsResponseDto MapResponse(
        ArticleInteractionStatsEntity stats)
    {
        return new GetArticleInteractionStatsResponseDto
        {
            ArticlePublicId = stats.ArticlePublicId,
            ViewCount = stats.ViewCount,
            LikeCount = stats.LikeCount,
            VisibleCommentCount = stats.VisibleCommentCount,
            StatsVersion = stats.StatsVersion,
            LastMaterializedAtUtc = stats.LastMaterializedAtUtc,
            LastPublishedAtUtc = stats.LastPublishedAtUtc
        };
    }
}