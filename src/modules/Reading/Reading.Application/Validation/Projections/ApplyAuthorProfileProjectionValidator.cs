using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Errors;
using Reading.Application.Models.Commands;

namespace Reading.Application.Validation.Projections;

public static class ApplyAuthorProfileProjectionValidator
{
    private const int PublicIdLength = 26;
    private const int MessageIdLength = 26;

    private const int AuthorDisplayNameMaxLength = 200;
    private const int AuthorAvatarUrlMaxLength = 800;

    public static Error? Validate(
        ApplyAuthorProfileProjectionCommand? command)
    {
        if (command is null)
        {
            return ReadingErrors.ValidationFailed;
        }

        if (command.AuthorUserId <= 0)
        {
            return ReadingErrors.ValidationFailed;
        }

        if (!HasExactLength(
                command.AuthorUserPublicId,
                PublicIdLength))
        {
            return ReadingErrors.ValidationFailed;
        }

        if (!HasValidOptionalLength(
                command.AuthorDisplayName,
                AuthorDisplayNameMaxLength))
        {
            return ReadingErrors.ValidationFailed;
        }

        if (!HasValidOptionalLength(
                command.AuthorAvatarUrl,
                AuthorAvatarUrlMaxLength))
        {
            return ReadingErrors.ValidationFailed;
        }

        if (command.SourceVersion <= 0)
        {
            return ReadingErrors.Projection.InvalidSourceVersion;
        }

        if (!HasExactLength(
                command.MessageId,
                MessageIdLength))
        {
            return ReadingErrors.Projection.InvalidMessageId;
        }

        if (command.SourceOccurredAtUtc == default)
        {
            return ReadingErrors.ValidationFailed;
        }

        return null;
    }

    public static ApplyAuthorProfileProjectionCommand Normalize(
        ApplyAuthorProfileProjectionCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return command with
        {
            AuthorUserPublicId = command.AuthorUserPublicId.Trim(),
            AuthorDisplayName = NormalizeNullable(command.AuthorDisplayName),
            AuthorAvatarUrl = NormalizeNullable(command.AuthorAvatarUrl),
            MessageId = command.MessageId.Trim()
        };
    }

    private static bool HasExactLength(
        string? value,
        int requiredLength)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.Trim().Length == requiredLength;
    }

    private static bool HasValidOptionalLength(
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