using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.Comments.RestoreComment;
using Interaction.Application.Errors;

namespace Interaction.Application.Validation.Comments;

public static class RestoreCommentValidator
{
    private const int PublicIdLength = 26;
    private const int MaximumNoteLength = 1000;

    public static Error? Validate(RestoreCommentRequestDto? request)
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

        if (request.Note is not null &&
            string.IsNullOrWhiteSpace(request.Note))
        {
            return InteractionErrors.Moderation.InvalidNote;
        }

        if (request.Note?.Trim().Length > MaximumNoteLength)
        {
            return InteractionErrors.Moderation.NoteTooLong;
        }

        return null;
    }

    public static string NormalizeCommentPublicId(string commentPublicId)
    {
        return commentPublicId.Trim();
    }

    public static string? NormalizeNote(string? note)
    {
        return string.IsNullOrWhiteSpace(note)
            ? null
            : note.Trim();
    }
}