using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.Likes.UnlikeArticle;

namespace Interaction.Application.UseCases.Likes.UnlikeArticle;

public interface IUnlikeArticleUseCase
{
    Task<Result<UnlikeArticleResponseDto>> ExecuteAsync(
        UnlikeArticleRequestDto request,
        CancellationToken cancellationToken = default);
}