using Notifications.Application.Contracts.Services;

namespace Notifications.Application.Ports.Services;

public interface INotificationTemplateRenderer
{
    Task<NotificationRenderResult> RenderAsync(
        NotificationRenderRequest request,
        CancellationToken cancellationToken = default);
}