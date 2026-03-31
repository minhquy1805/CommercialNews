using CommercialNews.BuildingBlocks.Results;
using Identity.Application.Contracts.Responses;

namespace Identity.Application.UseCases.GetMyProfile
{
    public interface IGetMyProfileUseCase
    {
        Task<Result<GetMyProfileResponseDto>> ExecuteAsync(
            CancellationToken cancellationToken = default);
    }
}