using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Consumers.Identity.Payloads;
using Notifications.Application.Contracts.Ingestion;

namespace Notifications.Application.Consumers.Identity;

public interface IIdentityEmailEventIngestionService
{
    Task<Result<NotificationIngestionResult>> IngestVerificationEmailRequestedAsync(
        string messageId,
        string? correlationId,
        IdentityVerificationEmailRequestedPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<NotificationIngestionResult>> IngestPasswordResetRequestedAsync(
        string messageId,
        string? correlationId,
        IdentityPasswordResetRequestedPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<NotificationIngestionResult>> IngestPasswordChangedAsync(
        string messageId,
        string? correlationId,
        IdentityPasswordChangedPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<NotificationIngestionResult>> IngestEmailVerifiedAsync(
        string messageId,
        string? correlationId,
        IdentityEmailVerifiedPayload payload,
        CancellationToken cancellationToken = default);
}