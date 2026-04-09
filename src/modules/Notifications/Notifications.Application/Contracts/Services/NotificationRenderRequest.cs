namespace Notifications.Application.Contracts.Services;

public sealed class NotificationRenderRequest
{
    public string TemplateKey { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string> Variables { get; init; }
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}