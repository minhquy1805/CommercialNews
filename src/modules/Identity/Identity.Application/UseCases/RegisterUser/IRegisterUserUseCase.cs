using Identity.Application.Contracts.Requests;
using Identity.Application.Contracts.Responses;

namespace Identity.Application.UseCases.RegisterUser
{
    public interface IRegisterUserUseCase
    {
        Task<RegisterUserResponseDto> ExecuteAsync(
           RegisterUserRequestDto request,
           CancellationToken cancellationToken);
    }
}
