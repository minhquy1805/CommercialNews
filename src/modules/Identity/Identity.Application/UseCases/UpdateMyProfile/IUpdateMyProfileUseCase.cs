using Identity.Application.Contracts.Requests;
using Identity.Application.Contracts.Responses;

namespace Identity.Application.UseCases.UpdateMyProfile
{
    public interface IUpdateMyProfileUseCase
    {
        Task<UpdateMyProfileResponseDto> ExecuteAsync(
            UpdateMyProfileRequestDto request,
            CancellationToken cancellationToken);
    }
}