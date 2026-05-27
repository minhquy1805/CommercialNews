using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.CommentReports.CreateCommentReport;
using Interaction.Application.Errors;
using Interaction.Domain.Constants;

namespace Interaction.Application.Validation.CommentReports;

public static class CreateCommentReportValidator
{
    private const int PublicIdLength = 26;
    private const int MaximumDescriptionLength = 1000;

    public static Error? Validate(CreateCommentReportRequestDto? request)
    {
        if (request is null)
        {
            return InteractionErrors.ValidationFailed;
        }

        if (string.IsNullOrWhiteSpace(request.CommentPublicId) ||
            request.CommentPublicId.Trim().Length != PublicIdLength)
        {
            return InteractionErrors.Comment.InvalidCommentPublicId;
        }

        if (string.IsNullOrWhiteSpace(request.ReasonCode))
        {
            return InteractionErrors.CommentReport.ReasonCodeRequired;
        }

        if (!CommentReportReasonCodes.IsValid(request.ReasonCode.Trim()))
        {
            return InteractionErrors.CommentReport.InvalidReasonCode;
        }

        if (request.Description is not null &&
            string.IsNullOrWhiteSpace(request.Description))
        {
            return InteractionErrors.CommentReport.InvalidDescription;
        }

        if (request.Description?.Trim().Length > MaximumDescriptionLength)
        {
            return InteractionErrors.CommentReport.DescriptionTooLong;
        }

        if (string.Equals(
                request.ReasonCode.Trim(),
                CommentReportReasonCodes.Other,
                StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(request.Description))
        {
            return InteractionErrors.CommentReport.DescriptionRequiredForOtherReason;
        }

        return null;
    }

    public static string NormalizeCommentPublicId(string commentPublicId)
    {
        return commentPublicId.Trim();
    }

    public static string NormalizeReasonCode(string reasonCode)
    {
        return reasonCode.Trim();
    }

    public static string? NormalizeDescription(string? description)
    {
        return string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();
    }
}