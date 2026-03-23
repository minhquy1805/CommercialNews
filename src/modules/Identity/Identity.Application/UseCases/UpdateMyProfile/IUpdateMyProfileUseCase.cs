using Identity.Application.Contracts.Dtos;

namespace Identity.Application.UseCases.UpdateMyProfile
{
    public interface IUpdateMyProfileUseCase
    {
        Task<UpdateMyProfileResponseDto> ExecuteAsync(
            UpdateMyProfileRequestDto request,
            CancellationToken cancellationToken);
    }
}