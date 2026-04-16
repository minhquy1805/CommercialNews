using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.LoginUser;

namespace Identity.Application.UseCases.LoginUser;

public interface ILoginUserUseCase
{
    Task<Result<LoginUserResponseDto>> ExecuteAsync(
        LoginUserRequestDto request,
        CancellationToken cancellationToken = default);
}