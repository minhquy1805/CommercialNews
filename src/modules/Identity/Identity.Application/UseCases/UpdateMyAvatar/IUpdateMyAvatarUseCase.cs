using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Identity.Application.Contracts.UpdateMyAvatar;

namespace Identity.Application.UseCases.UpdateMyAvatar;

public interface IUpdateMyAvatarUseCase
{
    Task<Result<UpdateMyAvatarResponseDto>> ExecuteAsync(
        UpdateMyAvatarRequestDto request,
        CancellationToken cancellationToken = default);
}
