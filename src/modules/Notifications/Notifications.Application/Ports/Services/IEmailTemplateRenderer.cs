using Notifications.Application.Contracts.Services;

namespace Notifications.Application.Ports.Services;

public interface IEmailTemplateRenderer
{
    Task<EmailTemplateRenderResult> RenderAsync(
        EmailTemplateRenderRequest request,
        CancellationToken cancellationToken = default);
}