using Identity.Application.Contracts.Requests;
using Identity.Application.Contracts.Responses;

namespace Identity.Application.UseCases.ChangePassword
{
    public interface IChangePasswordUseCase
    {
        Task<ChangePasswordResponseDto> ExecuteAsync(
            ChangePasswordRequestDto request,
            CancellationToken cancellationToken);
    }
}