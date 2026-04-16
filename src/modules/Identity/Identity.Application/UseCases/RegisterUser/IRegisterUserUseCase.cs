using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.RegisterUser;

namespace Identity.Application.UseCases.RegisterUser;

public interface IRegisterUserUseCase
{
    Task<Result<RegisterUserResponseDto>> ExecuteAsync(
        RegisterUserRequestDto request,
        CancellationToken cancellationToken = default);
}