using Notifications.Application.Contracts.Services;

namespace Notifications.Application.Ports.Services;

public interface IProviderResultClassifier
{
    ProviderClassificationResult Classify(
        EmailSendResult result);
}