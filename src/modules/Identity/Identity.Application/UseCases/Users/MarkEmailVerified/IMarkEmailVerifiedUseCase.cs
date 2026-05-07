using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.Users.MarkEmailVerified;

namespace Identity.Application.UseCases.Users.MarkEmailVerified;

public interface IMarkEmailVerifiedUseCase
{
    Task<Result<MarkEmailVerifiedResponseDto>> ExecuteAsync(
        MarkEmailVerifiedRequestDto request,
        CancellationToken cancellationToken = default);
}