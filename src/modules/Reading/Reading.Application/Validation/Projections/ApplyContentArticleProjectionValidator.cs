using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Errors;
using Reading.Application.Models.Commands;
using Reading.Domain.Constants;

namespace Reading.Application.Validation.Projections;

public static class ApplyContentArticleProjectionValidator
{
    private const int PublicIdLength = 26;
    private const int MessageIdLength = 26;

    private const int TitleMaxLength = 300;
    private const int SummaryMaxLength = 1000;
    private const int CategoryNameMaxLength = 200;
    private const int AuthorDisplayNameMaxLength = 200;
    private const int TagNameMaxLength = 150;
    private const int TagSlugMaxLength = 200;

    public static Error? Validate(
        ApplyContentArticleProjectionCommand? command)
    {
        if (command is null)
        {
            return ReadingErrors.ValidationFailed;
        }

        if (command.ArticleId <= 0)
        {
            return ReadingErrors.Article.InvalidArticleId;
        }

        if (!HasValidPublicId(command.ArticlePublicId))
        {
            return ReadingErrors.Article.InvalidArticlePublicId;
        }

        if (!HasValidRequiredText(command.Title, TitleMaxLength))
        {
            return ReadingErrors.ValidationFailed;
        }

        if (!HasValidRequiredText(command.Summary, SummaryMaxLength))
        {
            return ReadingErrors.ValidationFailed;
        }

        /*
          ArticleReadModel serves public article detail.
          A missing body must not be replaced with Summary because that would
          corrupt the projected article content.
        */
        if (string.IsNullOrWhiteSpace(command.Body))
        {
            return ReadingErrors.ValidationFailed;
        }

        if (command.CategoryId is <= 0)
        {
            return ReadingErrors.Query.InvalidCategoryId;
        }

        if (!HasValidOptionalText(
                command.CategoryName,
                CategoryNameMaxLength))
        {
            return ReadingErrors.ValidationFailed;
        }

        if (command.AuthorUserId is <= 0)
        {
            return ReadingErrors.ValidationFailed;
        }

        if (!HasValidOptionalText(
                command.AuthorDisplayName,
                AuthorDisplayNameMaxLength))
        {
            return ReadingErrors.ValidationFailed;
        }

        if (command.CoverMediaId is <= 0)
        {
            return ReadingErrors.ValidationFailed;
        }

        if (!HasValidTags(command.Tags))
        {
            return ReadingErrors.ValidationFailed;
        }

        if (!SourceArticleStatuses.IsValid(command.Status))
        {
            return ReadingErrors.Projection.InvalidSourceStatus;
        }

        if (!HasValidPublicVisibility(command))
        {
            return ReadingErrors.ValidationFailed;
        }

        if (command.UpdatedAtUtc == default)
        {
            return ReadingErrors.ValidationFailed;
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

    public static ApplyContentArticleProjectionCommand Normalize(
        ApplyContentArticleProjectionCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        string normalizedStatus =
            SourceArticleStatuses.NormalizeOrNull(command.Status)
            ?? command.Status.Trim();

        return command with
        {
            ArticlePublicId = command.ArticlePublicId.Trim(),
            Title = command.Title.Trim(),
            Summary = command.Summary.Trim(),

            /*
              Preserve article body content exactly as emitted by Content.
              Do not replace it with Summary and do not trim meaningful
              formatting from Markdown/HTML content.
            */
            Body = command.Body,

            CategoryName = NormalizeNullable(command.CategoryName),
            AuthorDisplayName = NormalizeNullable(command.AuthorDisplayName),
            Tags = command.Tags?
                .Select(tag => tag with
                {
                    TagPublicId = NormalizeNullable(tag.TagPublicId),
                    Name = tag.Name.Trim(),
                    Slug = NormalizeNullable(tag.Slug)
                })
                .ToArray(),
            Status = normalizedStatus,
            MessageId = NormalizeNullable(command.MessageId)
        };
    }

    private static bool HasValidPublicId(string? publicId)
    {
        return !string.IsNullOrWhiteSpace(publicId)
            && publicId.Trim().Length == PublicIdLength;
    }

    private static bool HasValidRequiredText(
        string? value,
        int maxLength)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.Trim().Length <= maxLength;
    }

    private static bool HasValidOptionalText(
        string? value,
        int maxLength)
    {
        return string.IsNullOrWhiteSpace(value)
            || value.Trim().Length <= maxLength;
    }

    private static bool HasValidMessageId(string? messageId)
    {
        return string.IsNullOrWhiteSpace(messageId)
            || messageId.Trim().Length == MessageIdLength;
    }

    private static bool HasValidPublicVisibility(
        ApplyContentArticleProjectionCommand command)
    {
        if (!command.IsPublic)
        {
            return true;
        }

        return SourceArticleStatuses.IsPublished(command.Status)
            && command.PublishedAtUtc.HasValue;
    }

    private static bool HasValidTags(
        IReadOnlyCollection<ArticleTagProjectionItem>? tags)
    {
        if (tags is null)
        {
            return true;
        }

        HashSet<long> tagIds = [];

        foreach (ArticleTagProjectionItem tag in tags)
        {
            if (tag.TagId <= 0
                || !HasValidOptionalPublicId(tag.TagPublicId)
                || !HasValidRequiredText(tag.Name, TagNameMaxLength)
                || !HasValidOptionalText(tag.Slug, TagSlugMaxLength)
                || !tagIds.Add(tag.TagId))
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasValidOptionalPublicId(string? publicId)
    {
        return string.IsNullOrWhiteSpace(publicId)
            || publicId.Trim().Length == PublicIdLength;
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
