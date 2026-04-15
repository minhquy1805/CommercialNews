using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;

namespace Content.Application.UseCases.Articles.CreateArticle
{
    public interface ICreateArticleUseCase
    {
        Task<Result<CreateArticleResponseDto>> ExecuteAsync(
            CreateArticleRequestDto request,
            CancellationToken cancellationToken = default);
    }
}
