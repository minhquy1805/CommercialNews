using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Errors;
using Reading.Application.Models.Commands;

namespace Reading.Application.Validation.Projections;

public static class ApplyArticleSeoMetadataProjectionValidator
{
    private const int PublicIdLength = 26;
    private const int MessageIdLength = 26;

    private const int ScopeMaxLength = 30;
    private const int ResourceTypeMaxLength = 50;

    private const int MetaTitleMaxLength = 300;
    private const int MetaDescriptionMaxLength = 500;

    private const int OgTitleMaxLength = 300;
    private const int OgDescriptionMaxLength = 500;
    private const int OgImageUrlMaxLength = 800;

    private const int TwitterTitleMaxLength = 300;
    private const int TwitterDescriptionMaxLength = 500;
    private const int TwitterImageUrlMaxLength = 800;

    private const int RobotsMaxLength = 100;

    public static Error? Validate(
        ApplyArticleSeoMetadataProjectionCommand? command)
    {
        if (command is null)
        {
            return ReadingErrors.ValidationFailed;
        }

        if (!HasValidRequiredText(command.Scope, ScopeMaxLength))
        {
            return ReadingErrors.ValidationFailed;
        }

        if (!HasValidRequiredText(command.ResourceType, ResourceTypeMaxLength))
        {
            return ReadingErrors.ValidationFailed;
        }

        if (!HasValidPublicId(command.ResourcePublicId))
        {
            return ReadingErrors.Article.InvalidArticlePublicId;
        }

        if (!HasValidOptionalText(command.MetaTitle, MetaTitleMaxLength)
            || !HasValidOptionalText(
                command.MetaDescription,
                MetaDescriptionMaxLength)
            || !HasValidOptionalText(command.OgTitle, OgTitleMaxLength)
            || !HasValidOptionalText(
                command.OgDescription,
                OgDescriptionMaxLength)
            || !HasValidOptionalText(command.OgImageUrl, OgImageUrlMaxLength)
            || !HasValidOptionalText(
                command.TwitterTitle,
                TwitterTitleMaxLength)
            || !HasValidOptionalText(
                command.TwitterDescription,
                TwitterDescriptionMaxLength)
            || !HasValidOptionalText(
                command.TwitterImageUrl,
                TwitterImageUrlMaxLength)
            || !HasValidOptionalText(command.Robots, RobotsMaxLength))
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

    public static ApplyArticleSeoMetadataProjectionCommand Normalize(
        ApplyArticleSeoMetadataProjectionCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return command with
        {
            Scope = command.Scope.Trim(),
            ResourceType = command.ResourceType.Trim(),
            ResourcePublicId = command.ResourcePublicId.Trim(),

            MetaTitle = NormalizeNullable(command.MetaTitle),
            MetaDescription = NormalizeNullable(command.MetaDescription),

            OgTitle = NormalizeNullable(command.OgTitle),
            OgDescription = NormalizeNullable(command.OgDescription),
            OgImageUrl = NormalizeNullable(command.OgImageUrl),

            TwitterTitle = NormalizeNullable(command.TwitterTitle),
            TwitterDescription = NormalizeNullable(
                command.TwitterDescription),
            TwitterImageUrl = NormalizeNullable(command.TwitterImageUrl),

            Robots = NormalizeNullable(command.Robots),
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