using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.UpdateMyProfile;

namespace Identity.Application.UseCases.UpdateMyProfile;

public interface IUpdateMyProfileUseCase
{
    Task<Result<UpdateMyProfileResponseDto>> ExecuteAsync(
        UpdateMyProfileRequestDto request,
        CancellationToken cancellationToken = default);
}