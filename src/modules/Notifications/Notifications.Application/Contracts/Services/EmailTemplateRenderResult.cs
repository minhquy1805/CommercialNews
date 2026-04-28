namespace Notifications.Application.Contracts.Services;

public sealed class EmailTemplateRenderResult
{
    public bool IsSuccess { get; init; }

    public string Subject { get; init; } = string.Empty;

    public string Body { get; init; } = string.Empty;

    public string? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }

    public static EmailTemplateRenderResult Success(
        string subject,
        string body)
    {
        return new EmailTemplateRenderResult
        {
            IsSuccess = true,
            Subject = subject,
            Body = body
        };
    }

    public static EmailTemplateRenderResult Failure(
        string errorCode,
        string errorMessage)
    {
        return new EmailTemplateRenderResult
        {
            IsSuccess = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }
}