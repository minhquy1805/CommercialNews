using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;

namespace Content.Application.UseCases.Tags.UpdateTag;

public interface IUpdateTagUseCase
{
    Task<Result<UpdateTagResponseDto>> ExecuteAsync(
        UpdateTagRequestDto request,
        CancellationToken cancellationToken = default);
}
