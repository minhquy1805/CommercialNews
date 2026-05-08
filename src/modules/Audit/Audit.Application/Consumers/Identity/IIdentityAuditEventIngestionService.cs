using Audit.Application.Consumers.Identity.Payloads;
using Audit.Application.Contracts.Ingestion;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Audit.Application.Consumers.Identity;

public interface IIdentityAuditEventIngestionService
{
    Task<Result<AuditIngestionResult>> IngestEmailVerifiedAsync(
        IdentityAuditEnvelopeContext context,
        EmailVerifiedAuditPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<AuditIngestionResult>> IngestPasswordChangedAsync(
        IdentityAuditEnvelopeContext context,
        PasswordChangedAuditPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<AuditIngestionResult>> IngestUserActivatedAsync(
        IdentityAuditEnvelopeContext context,
        UserActivatedAuditPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<AuditIngestionResult>> IngestUserDisabledAsync(
        IdentityAuditEnvelopeContext context,
        UserDisabledAuditPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<AuditIngestionResult>> IngestUserLockedAsync(
        IdentityAuditEnvelopeContext context,
        UserLockedAuditPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<AuditIngestionResult>> IngestUserUnlockedAsync(
        IdentityAuditEnvelopeContext context,
        UserUnlockedAuditPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<AuditIngestionResult>> IngestEmailMarkedVerifiedAsync(
        IdentityAuditEnvelopeContext context,
        EmailMarkedVerifiedAuditPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<AuditIngestionResult>> IngestUserSessionsRevokedAsync(
        IdentityAuditEnvelopeContext context,
        UserSessionsRevokedAuditPayload payload,
        CancellationToken cancellationToken = default);
}