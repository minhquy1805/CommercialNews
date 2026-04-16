using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.ChangePassword;

namespace Identity.Application.UseCases.ChangePassword;

public interface IChangePasswordUseCase
{
    Task<Result<ChangePasswordResponseDto>> ExecuteAsync(
        ChangePasswordRequestDto request,
        CancellationToken cancellationToken = default);
}