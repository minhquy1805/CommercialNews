using Notifications.Application.Contracts.Services;
using Notifications.Application.Ports.Services;
using Notifications.Domain.Enums;

namespace Notifications.Infrastructure.Services;

public sealed class NotificationTemplateRenderer : INotificationTemplateRenderer
{
    public Task<NotificationRenderResult> RenderAsync(
        NotificationRenderRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!NotificationTemplateKey.IsValid(request.TemplateKey))
        {
            return Task.FromResult(new NotificationRenderResult
            {
                IsSuccess = false,
                ErrorCode = NotificationServiceErrorCodes.TemplateKeyInvalid,
                ErrorMessage = "The notification template key is invalid."
            });
        }

        try
        {
            return Task.FromResult(request.TemplateKey switch
            {
                var key when string.Equals(
                    key,
                    NotificationTemplateKey.VerifyEmail,
                    StringComparison.OrdinalIgnoreCase)
                    => RenderVerifyEmail(request.Variables),

                var key when string.Equals(
                    key,
                    NotificationTemplateKey.ResetPassword,
                    StringComparison.OrdinalIgnoreCase)
                    => RenderResetPassword(request.Variables),

                var key when string.Equals(
                    key,
                    NotificationTemplateKey.NewArticle,
                    StringComparison.OrdinalIgnoreCase)
                    => RenderNewArticle(request.Variables),

                _ => new NotificationRenderResult
                {
                    IsSuccess = false,
                    ErrorCode = NotificationServiceErrorCodes.TemplateKeyInvalid,
                    ErrorMessage = "The notification template key is invalid."
                }
            });
        }
        catch (Exception exception)
        {
            // Important:
            // Rendering failures should be reported as structured template errors
            // instead of leaking raw rendering exceptions into the use-case layer.
            return Task.FromResult(new NotificationRenderResult
            {
                IsSuccess = false,
                ErrorCode = NotificationServiceErrorCodes.TemplateRenderFailed,
                ErrorMessage = exception.Message
            });
        }
    }

    private static NotificationRenderResult RenderVerifyEmail(
        IReadOnlyDictionary<string, string> variables)
    {
        // Important:
        // We validate required variables explicitly so the use case can treat
        // missing/unsafe template data as deterministic template failures.
        if (!TryGetRequiredVariable(variables, "UserName", out string userName, out NotificationRenderResult? failure) ||
            !TryGetRequiredVariable(variables, "VerificationUrl", out string verificationUrl, out failure))
        {
            return failure!;
        }

        string subject = "Verify your email address";

        string body =
$"""
Hello {userName},

Thank you for registering.

Please verify your email address by clicking the link below:
{verificationUrl}

If you did not request this email, you can safely ignore it.

CommercialNews
""";

        return Success(subject, body);
    }

    private static NotificationRenderResult RenderResetPassword(
        IReadOnlyDictionary<string, string> variables)
    {
        if (!TryGetRequiredVariable(variables, "UserName", out string userName, out NotificationRenderResult? failure) ||
            !TryGetRequiredVariable(variables, "ResetUrl", out string resetUrl, out failure))
        {
            return failure!;
        }

        string subject = "Reset your password";

        string body =
$"""
Hello {userName},

We received a request to reset your password.

You can reset it using the link below:
{resetUrl}

If you did not request a password reset, please ignore this email.

CommercialNews
""";

        return Success(subject, body);
    }

    private static NotificationRenderResult RenderNewArticle(
        IReadOnlyDictionary<string, string> variables)
    {
        if (!TryGetRequiredVariable(variables, "ArticleTitle", out string articleTitle, out NotificationRenderResult? failure) ||
            !TryGetRequiredVariable(variables, "ArticleUrl", out string articleUrl, out failure))
        {
            return failure!;
        }

        string? summary = TryGetOptionalVariable(variables, "Summary");

        string subject = $"New article published: {articleTitle}";

        string body =
$"""
A new article has been published.

Title: {articleTitle}
{(string.IsNullOrWhiteSpace(summary) ? string.Empty : $"Summary: {summary}")}

Read more:
{articleUrl}

CommercialNews
""";

        return Success(subject, body);
    }

    private static bool TryGetRequiredVariable(
        IReadOnlyDictionary<string, string> variables,
        string variableName,
        out string value,
        out NotificationRenderResult? failure)
    {
        failure = null;
        value = string.Empty;

        if (!variables.TryGetValue(variableName, out string? rawValue) ||
            string.IsNullOrWhiteSpace(rawValue))
        {
            failure = new NotificationRenderResult
            {
                IsSuccess = false,
                ErrorCode = NotificationServiceErrorCodes.TemplateRenderFailed,
                ErrorMessage = $"Required template variable '{variableName}' is missing."
            };

            return false;
        }

        // Important:
        // Phase 1 keeps variable safety simple.
        // We reject obviously unsafe control characters to reduce the chance
        // of malformed template output or header/body abuse.
        if (ContainsUnsafeControlCharacters(rawValue))
        {
            failure = new NotificationRenderResult
            {
                IsSuccess = false,
                ErrorCode = NotificationServiceErrorCodes.UnsafeTemplateVariables,
                ErrorMessage = $"Template variable '{variableName}' contains unsafe content."
            };

            return false;
        }

        value = rawValue.Trim();
        return true;
    }

    private static string? TryGetOptionalVariable(
        IReadOnlyDictionary<string, string> variables,
        string variableName)
    {
        if (!variables.TryGetValue(variableName, out string? rawValue) ||
            string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        if (ContainsUnsafeControlCharacters(rawValue))
        {
            return null;
        }

        return rawValue.Trim();
    }

    private static bool ContainsUnsafeControlCharacters(string value)
    {
        foreach (char character in value)
        {
            if (char.IsControl(character) &&
                character != '\r' &&
                character != '\n' &&
                character != '\t')
            {
                return true;
            }
        }

        return false;
    }

    private static NotificationRenderResult Success(string subject, string body)
    {
        return new NotificationRenderResult
        {
            IsSuccess = true,
            Subject = subject,
            Body = body,
            ErrorCode = null,
            ErrorMessage = null
        };
    }
}