using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Errors;
using Reading.Application.Models.Commands;
using Reading.Domain.Constants;

namespace Reading.Application.Validation.Projections;

public static class MarkArticleProjectionNotPublicValidator
{
    private const int MessageIdLength = 26;

    public static Error? Validate(MarkArticleProjectionNotPublicCommand? command)
    {
        if (command is null)
        {
            return ReadingErrors.ValidationFailed;
        }

        if (command.ArticleId <= 0)
        {
            return ReadingErrors.Article.InvalidArticleId;
        }

        if (string.IsNullOrWhiteSpace(command.Status))
        {
            return ReadingErrors.Projection.InvalidSourceStatus;
        }

        if (!SourceArticleStatuses.IsValid(command.Status))
        {
            return ReadingErrors.Projection.InvalidSourceStatus;
        }

        if (command.SourceVersion <= 0)
        {
            return ReadingErrors.Projection.InvalidSourceVersion;
        }

        if (!string.IsNullOrWhiteSpace(command.MessageId)
            && command.MessageId.Trim().Length != MessageIdLength)
        {
            return ReadingErrors.Projection.InvalidMessageId;
        }

        return null;
    }

    public static MarkArticleProjectionNotPublicCommand Normalize(
        MarkArticleProjectionNotPublicCommand command)
    {
        string? normalizedStatus = SourceArticleStatuses.NormalizeOrNull(
            command.Status);

        return command with
        {
            Status = normalizedStatus ?? command.Status.Trim(),
            MessageId = NormalizeNullable(command.MessageId)
        };
    }

    private static string? NormalizeNullable(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
