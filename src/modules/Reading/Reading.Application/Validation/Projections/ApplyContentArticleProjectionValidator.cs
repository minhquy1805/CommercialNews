using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Errors;
using Reading.Application.Models.Commands;
using Reading.Domain.Constants;

namespace Reading.Application.Validation.Projections;

public static class ApplyContentArticleProjectionValidator
{
    private const int PublicIdLength = 26;
    private const int MessageIdLength = 26;

    public static Error? Validate(ApplyContentArticleProjectionCommand? command)
    {
        if (command is null)
        {
            return ReadingErrors.ValidationFailed;
        }

        if (command.ArticleId <= 0)
        {
            return ReadingErrors.Article.InvalidArticleId;
        }

        if (string.IsNullOrWhiteSpace(command.ArticlePublicId)
            || command.ArticlePublicId.Trim().Length != PublicIdLength)
        {
            return ReadingErrors.Article.InvalidArticlePublicId;
        }

        if (string.IsNullOrWhiteSpace(command.Title))
        {
            return ReadingErrors.ValidationFailed;
        }

        if (string.IsNullOrWhiteSpace(command.Summary))
        {
            return ReadingErrors.ValidationFailed;
        }

        // Reading projection is tolerant:
        // Body may be missing from lightweight Content events.
        // Normalize(...) will fallback Body to Summary when Body is blank.

        if (command.CategoryId.HasValue && command.CategoryId.Value <= 0)
        {
            return ReadingErrors.Query.InvalidCategoryId;
        }

        if (command.AuthorUserId.HasValue && command.AuthorUserId.Value <= 0)
        {
            return ReadingErrors.ValidationFailed;
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

    public static ApplyContentArticleProjectionCommand Normalize(
        ApplyContentArticleProjectionCommand command)
    {
        string? normalizedStatus = SourceArticleStatuses.NormalizeOrNull(
            command.Status);

        string normalizedSummary = command.Summary.Trim();

        string normalizedBody = string.IsNullOrWhiteSpace(command.Body)
            ? normalizedSummary
            : command.Body.Trim();

        return command with
        {
            ArticlePublicId = command.ArticlePublicId.Trim(),
            Title = command.Title.Trim(),
            Summary = normalizedSummary,
            Body = normalizedBody,
            CategoryName = NormalizeNullable(command.CategoryName),
            AuthorDisplayName = NormalizeNullable(command.AuthorDisplayName),
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