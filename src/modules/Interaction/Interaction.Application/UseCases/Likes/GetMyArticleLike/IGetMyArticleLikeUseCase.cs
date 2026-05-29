using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.Likes.GetMyArticleLike;

namespace Interaction.Application.UseCases.Likes.GetMyArticleLike;

public interface IGetMyArticleLikeUseCase
{
    Task<Result<GetMyArticleLikeResponseDto>> ExecuteAsync(
        GetMyArticleLikeRequestDto request,
        CancellationToken cancellationToken = default);
}