using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notifications.Application.Contracts.Services;
using Notifications.Application.Ports.Services;
using Notifications.Domain.Enums;

namespace Notifications.Infrastructure.Services;

public sealed class EmailTemplateRenderer : IEmailTemplateRenderer
{
    private static readonly Regex PlaceholderRegex =
        new(@"\{\{\s*(?<name>[A-Za-z0-9_]+)\s*\}\}",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly EmailTemplateOptions _options;
    private readonly ILogger<EmailTemplateRenderer> _logger;

    public EmailTemplateRenderer(
        IOptions<EmailTemplateOptions> options,
        ILogger<EmailTemplateRenderer> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<EmailTemplateRenderResult> RenderAsync(
        EmailTemplateRenderRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string templateKey = request.TemplateKey?.Trim() ?? string.Empty;

        if (!NotificationTemplateKey.IsValid(templateKey))
        {
            return EmailTemplateRenderResult.Failure(
                errorCode: "TEMPLATE_KEY_INVALID",
                errorMessage: "Template key is invalid.");
        }

        try
        {
            string subjectPath = BuildTemplatePath(templateKey, "subject.txt");
            string bodyPath = BuildTemplatePath(templateKey, "body.html");

            if (!File.Exists(subjectPath))
            {
                return EmailTemplateRenderResult.Failure(
                    errorCode: "TEMPLATE_SUBJECT_NOT_FOUND",
                    errorMessage: "Email subject template was not found.");
            }

            if (!File.Exists(bodyPath))
            {
                return EmailTemplateRenderResult.Failure(
                    errorCode: "TEMPLATE_BODY_NOT_FOUND",
                    errorMessage: "Email body template was not found.");
            }

            string subjectTemplate = await File.ReadAllTextAsync(
                subjectPath,
                cancellationToken);

            string bodyTemplate = await File.ReadAllTextAsync(
                bodyPath,
                cancellationToken);

            string subject = RenderTemplate(
                subjectTemplate,
                request.Variables,
                htmlEncode: false);

            string body = RenderTemplate(
                bodyTemplate,
                request.Variables,
                htmlEncode: true);

            if (string.IsNullOrWhiteSpace(subject))
            {
                return EmailTemplateRenderResult.Failure(
                    errorCode: "TEMPLATE_SUBJECT_EMPTY",
                    errorMessage: "Rendered email subject is empty.");
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                return EmailTemplateRenderResult.Failure(
                    errorCode: "TEMPLATE_BODY_EMPTY",
                    errorMessage: "Rendered email body is empty.");
            }

            return EmailTemplateRenderResult.Success(
                subject: subject,
                body: body);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to render email template. TemplateKey={TemplateKey}",
                templateKey);

            return EmailTemplateRenderResult.Failure(
                errorCode: "TEMPLATE_RENDER_FAILED",
                errorMessage: "Email template rendering failed.");
        }
    }

    private string BuildTemplatePath(string templateKey, string suffix)
    {
        string fileName = $"{templateKey}.{suffix}";

        return Path.Combine(
            AppContext.BaseDirectory,
            _options.TemplateDirectory,
            fileName);
    }

    private static string RenderTemplate(
        string template,
        IReadOnlyDictionary<string, string> variables,
        bool htmlEncode)
    {
        return PlaceholderRegex.Replace(template, match =>
        {
            string name = match.Groups["name"].Value;

            if (!variables.TryGetValue(name, out string? value))
            {
                return string.Empty;
            }

            return htmlEncode
                ? WebUtility.HtmlEncode(value)
                : value;
        });
    }
}