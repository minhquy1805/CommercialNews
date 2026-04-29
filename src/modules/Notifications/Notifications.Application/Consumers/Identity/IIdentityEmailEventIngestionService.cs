using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Consumers.Identity.Payloads;

namespace Notifications.Application.Consumers.Identity;

public interface IIdentityEmailEventIngestionService
{
    Task<Result<long>> IngestVerificationEmailRequestedAsync(
        string messageId,
        string? correlationId,
        IdentityVerificationEmailRequestedPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<long>> IngestPasswordResetRequestedAsync(
        string messageId,
        string? correlationId,
        IdentityPasswordResetRequestedPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<long>> IngestPasswordChangedAsync(
        string messageId,
        string? correlationId,
        IdentityPasswordChangedPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<long>> IngestEmailVerifiedAsync(
        string messageId,
        string? correlationId,
        IdentityEmailVerifiedPayload payload,
        CancellationToken cancellationToken = default);
}