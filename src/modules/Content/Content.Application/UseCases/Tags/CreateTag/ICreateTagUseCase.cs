using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;

namespace Content.Application.UseCases.Tags.CreateTag
{
    public interface ICreateTagUseCase
    {
        Task<Result<CreateTagResponseDto>> ExecuteAsync(
            CreateTagRequestDto request,
            CancellationToken cancellationToken = default);
    }
}