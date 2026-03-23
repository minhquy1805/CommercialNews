using Identity.Application.Contracts.Dtos;

namespace Identity.Application.UseCases.GetMyProfile
{
    public interface IGetMyProfileUseCase
    {
        Task<GetMyProfileResponseDto> ExecuteAsync(CancellationToken cancellationToken);
    }
}