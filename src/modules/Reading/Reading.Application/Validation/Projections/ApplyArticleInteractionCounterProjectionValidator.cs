using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Errors;
using Reading.Application.Models.Commands;

namespace Reading.Application.Validation.Projections;

public static class ApplyArticleInteractionCounterProjectionValidator
{
    private const int PublicIdLength = 26;
    private const int MessageIdLength = 26;

    public static Error? Validate(
        ApplyArticleInteractionCounterProjectionCommand? command)
    {
        if (command is null)
        {
            return ReadingErrors.Projection.InteractionCounterCommandRequired;
        }

        if (string.IsNullOrWhiteSpace(command.ArticlePublicId)
            || command.ArticlePublicId.Trim().Length != PublicIdLength)
        {
            return ReadingErrors.Projection
                .InvalidInteractionCounterArticlePublicId;
        }

        if (command.ViewCount < 0
            || command.LikeCount < 0
            || command.VisibleCommentCount < 0)
        {
            return ReadingErrors.Projection.InvalidInteractionCounters;
        }

        if (command.InteractionStatsVersion <= 0)
        {
            return ReadingErrors.Projection.InvalidInteractionStatsVersion;
        }

        if (string.IsNullOrWhiteSpace(command.MessageId)
            || command.MessageId.Trim().Length != MessageIdLength)
        {
            return ReadingErrors.Projection.InvalidInteractionCounterMessageId;
        }

        if (command.SourceOccurredAtUtc == default)
        {
            return ReadingErrors.Projection
                .InteractionCounterSourceOccurredAtRequired;
        }

        return null;
    }

    public static ApplyArticleInteractionCounterProjectionCommand Normalize(
        ApplyArticleInteractionCounterProjectionCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return command with
        {
            ArticlePublicId = command.ArticlePublicId.Trim(),
            MessageId = command.MessageId.Trim()
        };
    }
}
