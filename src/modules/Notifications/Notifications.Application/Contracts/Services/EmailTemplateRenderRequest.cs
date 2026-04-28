namespace Notifications.Application.Contracts.Services;

public sealed class EmailTemplateRenderRequest
{
    public string TemplateKey { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string> Variables { get; init; }
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}