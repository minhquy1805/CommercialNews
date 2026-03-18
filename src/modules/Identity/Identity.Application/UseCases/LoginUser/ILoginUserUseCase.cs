using Identity.Application.Contracts.Dtos;

namespace Identity.Application.UseCases.LoginUser
{
    public interface ILoginUserUseCase
    {
        Task<LoginUserResponseDto> ExecuteAsync(
            LoginUserRequestDto request,
            CancellationToken cancellationToken);
    }
}
