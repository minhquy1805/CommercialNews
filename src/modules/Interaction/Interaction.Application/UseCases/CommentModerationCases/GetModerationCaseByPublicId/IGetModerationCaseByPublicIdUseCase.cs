using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.CommentModerationCases.GetModerationCaseByPublicId;

namespace Interaction.Application.UseCases.CommentModerationCases.GetModerationCaseByPublicId;

public interface IGetModerationCaseByPublicIdUseCase
{
    Task<Result<GetModerationCaseByPublicIdResponseDto>> ExecuteAsync(
        GetModerationCaseByPublicIdRequestDto request,
        CancellationToken cancellationToken = default);
}