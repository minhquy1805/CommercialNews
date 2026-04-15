using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.Views.Requests;
using Interaction.Application.Contracts.Views.Responses;

namespace Interaction.Application.UseCases.TrackArticleView;

public interface ITrackArticleViewUseCase
{
    Task<Result<TrackArticleViewResponse>> ExecuteAsync(
        TrackArticleViewRequest request,
        CancellationToken cancellationToken = default);
}