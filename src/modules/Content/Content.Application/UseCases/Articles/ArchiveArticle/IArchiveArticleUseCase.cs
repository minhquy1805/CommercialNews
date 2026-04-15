using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;

namespace Content.Application.UseCases.Articles.ArchiveArticle
{
    public interface IArchiveArticleUseCase
    {
        Task<Result<ArchiveArticleResponseDto>> ExecuteAsync(
            ArchiveArticleRequestDto request,
            CancellationToken cancellationToken = default);
    }
}

