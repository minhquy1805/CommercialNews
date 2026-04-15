using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.Requests;
using Identity.Application.Contracts.Responses;

namespace Identity.Application.UseCases.UpdateMyProfile
{
    public interface IUpdateMyProfileUseCase
    {
        Task<Result<UpdateMyProfileResponseDto>> ExecuteAsync(
            UpdateMyProfileRequestDto request,
            CancellationToken cancellationToken = default);
    }
}