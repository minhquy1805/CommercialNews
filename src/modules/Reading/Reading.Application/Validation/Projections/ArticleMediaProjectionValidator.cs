using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Errors;
using Reading.Application.Models.Commands;

namespace Reading.Application.Validation.Projections;

public static class ArticleMediaProjectionValidator
{
    private const int PublicIdLength = 26;
    private const int MessageIdLength = 26;

    private const int UrlMaxLength = 1000;
    private const int AltMaxLength = 300;
    private const int CaptionMaxLength = 300;
    private const int MediaTypeMaxLength = 50;

    public static Error? Validate(
        UpsertArticleMediaProjectionCommand? command)
    {
        if (command is null)
        {
            return ReadingErrors.ValidationFailed;
        }

        if (!HasValidArticleId(command.ArticleId)
            || !HasValidMediaId(command.MediaId)
            || !HasValidPublicId(command.MediaPublicId)
            || !HasValidRequiredText(command.Url, UrlMaxLength)
            || !HasValidOptionalText(command.Alt, AltMaxLength)
            || !HasValidOptionalText(command.Caption, CaptionMaxLength)
            || !HasValidRequiredText(command.MediaType, MediaTypeMaxLength)
            || command.SortOrder < 0
            || !HasValidSourceVersion(command.SourceVersion)
            || !HasValidMessageId(command.MessageId))
        {
            return ReadingErrors.ValidationFailed;
        }

        return null;
    }

    public static Error? Validate(
        SetPrimaryArticleMediaProjectionCommand? command)
    {
        if (command is null)
        {
            return ReadingErrors.ValidationFailed;
        }

        if (!HasValidArticleId(command.ArticleId)
            || !HasValidMediaId(command.MediaId)
            || !HasValidPublicId(command.MediaPublicId)
            || !HasValidRequiredText(command.Url, UrlMaxLength)
            || !HasValidOptionalText(command.Alt, AltMaxLength)
            || !HasValidOptionalText(command.Caption, CaptionMaxLength)
            || !HasValidRequiredText(command.MediaType, MediaTypeMaxLength)
            || command.SortOrder < 0
            || !HasValidSourceVersion(command.SourceVersion)
            || !HasValidMessageId(command.MessageId))
        {
            return ReadingErrors.ValidationFailed;
        }

        return null;
    }

    public static Error? Validate(
        ReorderArticleMediaProjectionCommand? command)
    {
        if (command is null)
        {
            return ReadingErrors.ValidationFailed;
        }

        if (!HasValidArticleId(command.ArticleId)
            || command.Items is null
            || command.Items.Count == 0
            || command.Items.Any(item =>
                !HasValidMediaId(item.MediaId)
                || item.SortOrder < 0)
            || command.Items
                .Select(item => item.MediaId)
                .Distinct()
                .Count() != command.Items.Count
            || !HasValidSourceVersion(command.SourceVersion)
            || !HasValidMessageId(command.MessageId))
        {
            return ReadingErrors.ValidationFailed;
        }

        return null;
    }

    public static Error? Validate(
        DetachArticleMediaProjectionCommand? command)
    {
        if (command is null)
        {
            return ReadingErrors.ValidationFailed;
        }

        if (!HasValidArticleId(command.ArticleId)
            || !HasValidMediaId(command.MediaId)
            || !HasValidSourceVersion(command.SourceVersion)
            || !HasValidMessageId(command.MessageId))
        {
            return ReadingErrors.ValidationFailed;
        }

        return null;
    }

    public static UpsertArticleMediaProjectionCommand Normalize(
        UpsertArticleMediaProjectionCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return command with
        {
            MediaPublicId = command.MediaPublicId.Trim(),
            Url = command.Url.Trim(),
            Alt = NormalizeNullable(command.Alt),
            Caption = NormalizeNullable(command.Caption),
            MediaType = command.MediaType.Trim(),
            MessageId = NormalizeNullable(command.MessageId)
        };
    }

    public static SetPrimaryArticleMediaProjectionCommand Normalize(
        SetPrimaryArticleMediaProjectionCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return command with
        {
            MediaPublicId = command.MediaPublicId.Trim(),
            Url = command.Url.Trim(),
            Alt = NormalizeNullable(command.Alt),
            Caption = NormalizeNullable(command.Caption),
            MediaType = command.MediaType.Trim(),
            MessageId = NormalizeNullable(command.MessageId)
        };
    }

    public static ReorderArticleMediaProjectionCommand Normalize(
        ReorderArticleMediaProjectionCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return command with
        {
            Items = command.Items
                .Select(static item => new ArticleMediaProjectionOrderItem(
                    MediaId: item.MediaId,
                    SortOrder: item.SortOrder))
                .ToArray(),
            MessageId = NormalizeNullable(command.MessageId)
        };
    }

    public static DetachArticleMediaProjectionCommand Normalize(
        DetachArticleMediaProjectionCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return command with
        {
            MessageId = NormalizeNullable(command.MessageId)
        };
    }

    private static bool HasValidArticleId(long articleId)
    {
        return articleId > 0;
    }

    private static bool HasValidMediaId(long mediaId)
    {
        return mediaId > 0;
    }

    private static bool HasValidPublicId(string? publicId)
    {
        return !string.IsNullOrWhiteSpace(publicId)
            && publicId.Trim().Length == PublicIdLength;
    }

    private static bool HasValidSourceVersion(long sourceVersion)
    {
        return sourceVersion > 0;
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