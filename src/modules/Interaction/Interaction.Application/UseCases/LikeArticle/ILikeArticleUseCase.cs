using CommercialNews.BuildingBlocks.Results;
using Interaction.Application.Contracts.Likes.Requests;
using Interaction.Application.Contracts.Likes.Responses;

namespace Interaction.Application.UseCases.LikeArticle;

public interface ILikeArticleUseCase
{
    Task<Result<LikeArticleResponse>> ExecuteAsync(
        LikeArticleRequest request,
        CancellationToken cancellationToken = default);
}