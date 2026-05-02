using Audit.Application.Consumers.Authorization.Payloads;
using Audit.Application.Contracts.Ingestion;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Audit.Application.Consumers.Authorization;

public interface IAuthorizationAuditEventIngestionService
{
    Task<Result<AuditIngestionResult>> IngestUserRoleAssignedAsync(
        AuthorizationAuditEnvelopeContext context,
        UserRoleAssignedAuditPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<AuditIngestionResult>> IngestUserRoleRevokedAsync(
        AuthorizationAuditEnvelopeContext context,
        UserRoleRevokedAuditPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<AuditIngestionResult>> IngestRolePermissionGrantedAsync(
        AuthorizationAuditEnvelopeContext context,
        RolePermissionGrantedAuditPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<AuditIngestionResult>> IngestRolePermissionRevokedAsync(
        AuthorizationAuditEnvelopeContext context,
        RolePermissionRevokedAuditPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<AuditIngestionResult>> IngestRoleCreatedAsync(
        AuthorizationAuditEnvelopeContext context,
        RoleCreatedAuditPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<AuditIngestionResult>> IngestRoleUpdatedAsync(
        AuthorizationAuditEnvelopeContext context,
        RoleUpdatedAuditPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<AuditIngestionResult>> IngestRoleActivatedAsync(
        AuthorizationAuditEnvelopeContext context,
        RoleActivatedAuditPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<AuditIngestionResult>> IngestRoleDeactivatedAsync(
        AuthorizationAuditEnvelopeContext context,
        RoleDeactivatedAuditPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<AuditIngestionResult>> IngestPermissionCreatedAsync(
        AuthorizationAuditEnvelopeContext context,
        PermissionCreatedAuditPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<AuditIngestionResult>> IngestPermissionUpdatedAsync(
        AuthorizationAuditEnvelopeContext context,
        PermissionUpdatedAuditPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<AuditIngestionResult>> IngestPermissionActivatedAsync(
        AuthorizationAuditEnvelopeContext context,
        PermissionActivatedAuditPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<AuditIngestionResult>> IngestPermissionDeactivatedAsync(
        AuthorizationAuditEnvelopeContext context,
        PermissionDeactivatedAuditPayload payload,
        CancellationToken cancellationToken = default);
}