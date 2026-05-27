using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.Comments.HideComment;
using Interaction.Application.Errors;
using Interaction.Domain.Constants;

namespace Interaction.Application.Validation.Comments;

public static class HideCommentValidator
{
    private const int PublicIdLength = 26;
    private const int MaximumNoteLength = 1000;

    public static Error? Validate(HideCommentRequestDto? request)
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

        if (request.ExpectedVersion < 1)
        {
            return InteractionErrors.Comment.InvalidExpectedVersion;
        }

        if (string.IsNullOrWhiteSpace(request.ReasonCode))
        {
            return InteractionErrors.Moderation.ReasonCodeRequired;
        }

        if (!ModerationReasonCodes.IsValid(request.ReasonCode.Trim()))
        {
            return InteractionErrors.Moderation.InvalidReasonCode;
        }

        if (request.Note is not null &&
            string.IsNullOrWhiteSpace(request.Note))
        {
            return InteractionErrors.Moderation.InvalidNote;
        }

        if (request.Note?.Trim().Length > MaximumNoteLength)
        {
            return InteractionErrors.Moderation.NoteTooLong;
        }

        if (string.Equals(
                request.ReasonCode.Trim(),
                ModerationReasonCodes.Other,
                StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(request.Note))
        {
            return InteractionErrors.Moderation.NoteRequiredForOtherReason;
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

    public static string? NormalizeNote(string? note)
    {
        return string.IsNullOrWhiteSpace(note)
            ? null
            : note.Trim();
    }
}