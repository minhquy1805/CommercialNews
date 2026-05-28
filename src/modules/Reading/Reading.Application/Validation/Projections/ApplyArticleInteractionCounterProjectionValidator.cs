using CommercialNews.BuildingBlocks.SharedKernel.Results;
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
            return Error.Validation(
                code: "READING.INTERACTION_COUNTER.COMMAND_REQUIRED",
                message: "Interaction counter projection command is required.");
        }

        if (string.IsNullOrWhiteSpace(command.ArticlePublicId)
            || command.ArticlePublicId.Trim().Length != PublicIdLength)
        {
            return Error.Validation(
                code: "READING.INTERACTION_COUNTER.ARTICLE_PUBLIC_ID_INVALID",
                message: "Article public id must be a valid 26-character value.");
        }

        if (command.ViewCount < 0
            || command.LikeCount < 0
            || command.VisibleCommentCount < 0)
        {
            return Error.Validation(
                code: "READING.INTERACTION_COUNTER.COUNTERS_INVALID",
                message: "Interaction counters must be non-negative.");
        }

        if (command.InteractionStatsVersion <= 0)
        {
            return Error.Validation(
                code: "READING.INTERACTION_COUNTER.STATS_VERSION_INVALID",
                message: "Interaction stats version must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(command.MessageId)
            || command.MessageId.Trim().Length != MessageIdLength)
        {
            return Error.Validation(
                code: "READING.INTERACTION_COUNTER.MESSAGE_ID_INVALID",
                message: "Message id must be a valid 26-character value.");
        }

        if (command.SourceOccurredAtUtc == default)
        {
            return Error.Validation(
                code: "READING.INTERACTION_COUNTER.SOURCE_OCCURRED_AT_REQUIRED",
                message: "Source occurred timestamp is required.");
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