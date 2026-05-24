using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Errors;
using Reading.Application.Models.Commands;

namespace Reading.Application.Validation.Projections;

public static class ApplyArticleSeoRouteProjectionValidator
{
    private const int PublicIdLength = 26;
    private const int MessageIdLength = 26;

    private const int ScopeMaxLength = 30;
    private const int ResourceTypeMaxLength = 50;
    private const int SlugMaxLength = 200;
    private const int CanonicalUrlMaxLength = 500;

    public static Error? Validate(
        ApplyArticleSeoRouteProjectionCommand? command)
    {
        if (command is null)
        {
            return ReadingErrors.ValidationFailed;
        }

        if (!HasValidRequiredText(command.Scope, ScopeMaxLength))
        {
            return ReadingErrors.ValidationFailed;
        }

        if (!HasValidRequiredText(
                command.ResourceType,
                ResourceTypeMaxLength))
        {
            return ReadingErrors.ValidationFailed;
        }

        if (!HasValidPublicId(command.ResourcePublicId))
        {
            return ReadingErrors.Article.InvalidArticlePublicId;
        }

        if (!HasValidRequiredText(command.Slug, SlugMaxLength))
        {
            return ReadingErrors.ValidationFailed;
        }

        if (!HasValidOptionalText(
                command.CanonicalUrl,
                CanonicalUrlMaxLength))
        {
            return ReadingErrors.ValidationFailed;
        }

        /*
          An indexable public route must also be active.
          This mirrors the Reading projection table invariant.
        */
        if (command.IsIndexable && !command.IsActive)
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

    public static ApplyArticleSeoRouteProjectionCommand Normalize(
        ApplyArticleSeoRouteProjectionCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return command with
        {
            Scope = command.Scope.Trim(),
            ResourceType = command.ResourceType.Trim(),
            ResourcePublicId = command.ResourcePublicId.Trim(),
            Slug = command.Slug.Trim(),
            CanonicalUrl = NormalizeNullable(command.CanonicalUrl),
            MessageId = NormalizeNullable(command.MessageId)
        };
    }

    private static bool HasValidPublicId(string? publicId)
    {
        return !string.IsNullOrWhiteSpace(publicId)
            && publicId.Trim().Length == PublicIdLength;
    }

    private static bool HasValidMessageId(string? messageId)
    {
        return string.IsNullOrWhiteSpace(messageId)
            || messageId.Trim().Length == MessageIdLength;
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

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}