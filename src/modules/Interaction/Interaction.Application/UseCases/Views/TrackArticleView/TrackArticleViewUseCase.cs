using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.Views.TrackArticleView;
using Interaction.Application.Ports.Persistence;
using Interaction.Application.Ports.Services;
using Interaction.Application.Validation.Views;

namespace Interaction.Application.UseCases.Views.TrackArticleView;

public sealed class TrackArticleViewUseCase : ITrackArticleViewUseCase
{
    private readonly IArticleViewCountRepository _articleViewCountRepository;
    private readonly IArticleViewAcceptancePolicy _articleViewAcceptancePolicy;

    public TrackArticleViewUseCase(
        IArticleViewCountRepository articleViewCountRepository,
        IArticleViewAcceptancePolicy articleViewAcceptancePolicy)
    {
        _articleViewCountRepository = articleViewCountRepository
            ?? throw new ArgumentNullException(nameof(articleViewCountRepository));

        _articleViewAcceptancePolicy = articleViewAcceptancePolicy
            ?? throw new ArgumentNullException(nameof(articleViewAcceptancePolicy));
    }

    public async Task<Result<TrackArticleViewResponseDto>> ExecuteAsync(
        TrackArticleViewRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationError = TrackArticleViewValidator.Validate(request);

        if (validationError is not null)
        {
            return Result<TrackArticleViewResponseDto>.Failure(validationError);
        }

        var articlePublicId =
            TrackArticleViewValidator.NormalizeArticlePublicId(
                request.ArticlePublicId);

        var acceptanceResult = await _articleViewAcceptancePolicy.EvaluateAsync(
            articlePublicId,
            cancellationToken);

        if (acceptanceResult.Error is not null)
        {
            return Result<TrackArticleViewResponseDto>.Failure(
                acceptanceResult.Error);
        }

        if (acceptanceResult.ShouldIncrementCount)
        {
            await _articleViewCountRepository.IncrementAcceptedAsync(
                articlePublicId,
                cancellationToken);
        }

        return Result<TrackArticleViewResponseDto>.Success(
            new TrackArticleViewResponseDto
            {
                Accepted = true
            });
    }
}