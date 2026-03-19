using Identity.Application.Contracts.Dtos;

namespace Identity.Application.UseCases.ResetPassword
{
    public interface IResetPasswordUseCase
    {
        Task<ResetPasswordResponseDto> ExecuteAsync(
            ResetPasswordRequestDto request,
            CancellationToken cancellationToken);
    }
}

