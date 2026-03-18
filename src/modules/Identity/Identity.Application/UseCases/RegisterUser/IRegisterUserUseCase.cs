using Identity.Application.Contracts.Dtos;

namespace Identity.Application.UseCases.RegisterUser
{
    public interface IRegisterUserUseCase
    {
        Task<RegisterUserResponseDto> ExecuteAsync(
           RegisterUserRequestDto request,
           CancellationToken cancellationToken);
    }
}
