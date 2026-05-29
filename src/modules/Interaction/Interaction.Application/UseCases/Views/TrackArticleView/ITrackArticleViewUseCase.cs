using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.Views.TrackArticleView;

namespace Interaction.Application.UseCases.Views.TrackArticleView;

public interface ITrackArticleViewUseCase
{
    Task<Result<TrackArticleViewResponseDto>> ExecuteAsync(
        TrackArticleViewRequestDto request,
        CancellationToken cancellationToken = default);
}