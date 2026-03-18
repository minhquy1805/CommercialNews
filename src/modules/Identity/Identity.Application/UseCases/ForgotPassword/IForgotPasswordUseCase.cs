using Identity.Application.Contracts.Dtos;

namespace Identity.Application.UseCases.ForgotPassword
{
    public interface IForgotPasswordUseCase
    {
        Task<ForgotPasswordResponseDto> ExecuteAsync(
            ForgotPasswordRequestDto request,
            CancellationToken cancellationToken);
    }
}
