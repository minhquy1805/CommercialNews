using Identity.Application.Contracts.Responses;

namespace Identity.Application.UseCases.GetMyProfile
{
    public interface IGetMyProfileUseCase
    {
        Task<GetMyProfileResponseDto> ExecuteAsync(CancellationToken cancellationToken);
    }
}