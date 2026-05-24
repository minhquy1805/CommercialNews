using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Errors;
using Reading.Application.Models.Commands;
using Reading.Domain.Constants;

namespace Reading.Application.Validation.Projections;

public static class MarkArticleProjectionNotPublicValidator
{
    private const int MessageIdLength = 26;

    public static Error? Validate(
        MarkArticleProjectionNotPublicCommand? command)
    {
        if (command is null)
        {
            return ReadingErrors.ValidationFailed;
        }

        if (command.ArticleId <= 0)
        {
            return ReadingErrors.Article.InvalidArticleId;
        }

        if (!SourceArticleStatuses.IsValid(command.Status))
        {
            return ReadingErrors.Projection.InvalidSourceStatus;
        }

        if (command.SourceVersion <= 0)
        {
            return ReadingErrors.Projection.InvalidSourceVersion;
        }

        if (!HasValidMessageId(command.MessageId))
        {
            return ReadingErrors.Projection.InvalidMessageId;
        }

        return null;
    }

    public static MarkArticleProjectionNotPublicCommand Normalize(
        MarkArticleProjectionNotPublicCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        string normalizedStatus =
            SourceArticleStatuses.NormalizeOrNull(command.Status)
            ?? command.Status.Trim();

        return command with
        {
            Status = normalizedStatus,
            MessageId = NormalizeNullable(command.MessageId)
        };
    }

    private static bool HasValidMessageId(string? messageId)
    {
        return string.IsNullOrWhiteSpace(messageId)
            || messageId.Trim().Length == MessageIdLength;
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}