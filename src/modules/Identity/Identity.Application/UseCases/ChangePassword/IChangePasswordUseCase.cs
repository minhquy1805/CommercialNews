using Identity.Application.Contracts.Dtos;

namespace Identity.Application.UseCases.ChangePassword
{
    public interface IChangePasswordUseCase
    {
        Task<ChangePasswordResponseDto> ExecuteAsync(
            ChangePasswordRequestDto request,
            CancellationToken cancellationToken);
    }
}