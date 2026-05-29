using System.Text.Json;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Microsoft.Extensions.Options;
using Notifications.Application.Configuration;
using Notifications.Application.Consumers.Interaction.Payloads;
using Notifications.Application.Contracts.Ingestion;
using Notifications.Application.Errors;
using Notifications.Application.Ports.Services;
using Notifications.Domain.Enums;

namespace Notifications.Application.Consumers.Interaction;

public sealed class InteractionNotificationEventIngestionService
    : IInteractionNotificationEventIngestionService
{
    private const string SourceModule = "Interaction";
    private const string CommentReportAlertTriggeredEventType =
        "interaction.comment_report_alert_triggered";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly INotificationIngestionService _notificationIngestionService;
    private readonly EmailDeliveryOptions _deliveryOptions;
    private readonly InteractionAlertEmailOptions _alertOptions;

    public InteractionNotificationEventIngestionService(
        INotificationIngestionService notificationIngestionService,
        IOptions<EmailDeliveryOptions> deliveryOptions,
        IOptions<InteractionAlertEmailOptions> alertOptions)
    {
        _notificationIngestionService = notificationIngestionService
            ?? throw new ArgumentNullException(nameof(notificationIngestionService));

        _deliveryOptions = deliveryOptions?.Value
            ?? throw new ArgumentNullException(nameof(deliveryOptions));

        _alertOptions = alertOptions?.Value
            ?? throw new ArgumentNullException(nameof(alertOptions));
    }

    public Task<Result<NotificationIngestionResult>>
        IngestCommentReportAlertTriggeredAsync(
            string messageId,
            string? correlationId,
            CommentReportAlertTriggeredNotificationPayload payload,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);

        if (string.IsNullOrWhiteSpace(messageId)
            || string.IsNullOrWhiteSpace(_alertOptions.CommentReportAlertRecipientEmail)
            || string.IsNullOrWhiteSpace(_alertOptions.ModerationCaseUrlTemplate)
            || string.IsNullOrWhiteSpace(_deliveryOptions.Provider)
            || string.IsNullOrWhiteSpace(payload.CommentModerationCasePublicId)
            || string.IsNullOrWhiteSpace(payload.CommentPublicId)
            || string.IsNullOrWhiteSpace(payload.ArticlePublicId)
            || string.IsNullOrWhiteSpace(payload.AlertLevel)
            || string.IsNullOrWhiteSpace(payload.AlertReason)
            || string.IsNullOrWhiteSpace(payload.HighestSeverity)
            || payload.DistinctReporterCount < 1
            || payload.TriggeredAtUtc == default)
        {
            return Task.FromResult(
                Result<NotificationIngestionResult>.Failure(
                    NotificationsErrors.ValidationFailed));
        }

        string moderationCaseUrl = BuildModerationCaseUrl(
            payload.CommentModerationCasePublicId);

        string variablesJson = BuildVariablesJson(new Dictionary<string, string?>
        {
            ["commentModerationCasePublicId"] =
                payload.CommentModerationCasePublicId,
            ["commentPublicId"] = payload.CommentPublicId,
            ["articlePublicId"] = payload.ArticlePublicId,
            ["alertLevel"] = payload.AlertLevel,
            ["alertReason"] = payload.AlertReason,
            ["distinctReporterCount"] =
                payload.DistinctReporterCount.ToString(),
            ["highestSeverity"] = payload.HighestSeverity,
            ["triggeredAtUtc"] = payload.TriggeredAtUtc.ToString("O"),
            ["moderationCaseUrl"] = moderationCaseUrl
        });

        return _notificationIngestionService.IngestEmailAsync(
            new EmailNotificationIngestionRequest
            {
                MessageId = messageId.Trim(),
                BusinessDedupeKey = BuildBusinessDedupeKey(
                    payload.CommentModerationCasePublicId),
                RecipientUserId = null,
                ToEmail = _alertOptions.CommentReportAlertRecipientEmail.Trim(),
                TemplateKey = NotificationTemplateKey.CommentReportAlert,
                VariablesJson = variablesJson,
                Provider = _deliveryOptions.Provider,
                Priority = _alertOptions.CommentReportAlertPriority,
                CorrelationId = correlationId,
                SourceModule = SourceModule,
                SourceEventType = CommentReportAlertTriggeredEventType,
                OccurredAtUtc = payload.TriggeredAtUtc
            },
            cancellationToken);
    }

    private string BuildModerationCaseUrl(string casePublicId)
    {
        return _alertOptions.ModerationCaseUrlTemplate.Trim().Replace(
            "{casePublicId}",
            Uri.EscapeDataString(casePublicId.Trim()),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildBusinessDedupeKey(string casePublicId)
    {
        return "notifications:interaction:comment-report-alert:" +
               casePublicId.Trim();
    }

    private static string BuildVariablesJson(
        IReadOnlyDictionary<string, string?> variables)
    {
        Dictionary<string, string> normalizedVariables = variables
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
            .ToDictionary(
                pair => pair.Key,
                pair => pair.Value!,
                StringComparer.OrdinalIgnoreCase);

        return JsonSerializer.Serialize(normalizedVariables, JsonOptions);
    }
}
