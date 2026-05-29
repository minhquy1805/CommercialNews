using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.CommentModerationCases.GetModerationCaseByPublicId;
using Interaction.Application.Errors;

namespace Interaction.Application.Validation.CommentModerationCases;

public static class GetModerationCaseByPublicIdValidator
{
    private const int PublicIdLength = 26;

    public static Error? Validate(
        GetModerationCaseByPublicIdRequestDto? request)
    {
        if (request is null)
        {
            return InteractionErrors.ValidationFailed;
        }

        if (string.IsNullOrWhiteSpace(request.CasePublicId) ||
            request.CasePublicId.Trim().Length != PublicIdLength)
        {
            return InteractionErrors.CommentModerationCase.InvalidCasePublicId;
        }

        return null;
    }

    public static string NormalizeCasePublicId(string casePublicId)
    {
        return casePublicId.Trim();
    }
}