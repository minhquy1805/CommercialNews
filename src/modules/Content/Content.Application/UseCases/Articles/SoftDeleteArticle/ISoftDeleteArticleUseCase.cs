using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;

namespace Content.Application.UseCases.Articles.SoftDeleteArticle;

public interface ISoftDeleteArticleUseCase
{
    Task<Result<SoftDeleteArticleResponseDto>> ExecuteAsync(
        SoftDeleteArticleRequestDto request,
        CancellationToken cancellationToken = default);
}
