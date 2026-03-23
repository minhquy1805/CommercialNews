using Identity.Application.Contracts.Requests;
using Identity.Application.Contracts.Responses;

namespace Identity.Application.UseCases.ResetPassword
{
    public interface IResetPasswordUseCase
    {
        Task<ResetPasswordResponseDto> ExecuteAsync(
            ResetPasswordRequestDto request,
            CancellationToken cancellationToken);
    }
}

