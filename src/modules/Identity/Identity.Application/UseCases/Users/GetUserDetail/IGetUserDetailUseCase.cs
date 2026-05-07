using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.Users.GetUserDetail;

namespace Identity.Application.UseCases.Users.GetUserDetail;

public interface IGetUserDetailUseCase
{
    Task<Result<GetUserDetailResponseDto>> ExecuteAsync(
        GetUserDetailRequestDto request,
        CancellationToken cancellationToken = default);
}