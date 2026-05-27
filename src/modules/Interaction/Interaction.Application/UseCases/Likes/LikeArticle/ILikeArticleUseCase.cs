using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.Likes.LikeArticle;

namespace Interaction.Application.UseCases.Likes.LikeArticle;

public interface ILikeArticleUseCase
{
    Task<Result<LikeArticleResponseDto>> ExecuteAsync(
        LikeArticleRequestDto request,
        CancellationToken cancellationToken = default);
}