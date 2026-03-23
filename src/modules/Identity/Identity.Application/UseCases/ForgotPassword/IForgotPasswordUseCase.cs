using Identity.Application.Contracts.Requests;
using Identity.Application.Contracts.Responses;

namespace Identity.Application.UseCases.ForgotPassword
{
    public interface IForgotPasswordUseCase
    {
        Task<ForgotPasswordResponseDto> ExecuteAsync(
            ForgotPasswordRequestDto request,
            CancellationToken cancellationToken);
    }
}
