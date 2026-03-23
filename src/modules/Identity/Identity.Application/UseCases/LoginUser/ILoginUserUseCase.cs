using Identity.Application.Contracts.Requests;
using Identity.Application.Contracts.Responses;

namespace Identity.Application.UseCases.LoginUser
{
    public interface ILoginUserUseCase
    {
        Task<LoginUserResponseDto> ExecuteAsync(
            LoginUserRequestDto request,
            CancellationToken cancellationToken);
    }
}
