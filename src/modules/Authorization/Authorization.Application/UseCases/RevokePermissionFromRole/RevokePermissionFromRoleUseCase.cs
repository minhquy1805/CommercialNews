using System.Text.Json;
using Authorization.Application.Contracts.Events;
using Authorization.Application.Contracts.Ports;
using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using CommercialNews.BuildingBlocks.Messaging.Outbox;

namespace Authorization.Application.UseCases.RevokePermissionFromRole
{
    public sealed class RevokePermissionFromRoleUseCase : IRevokePermissionFromRoleUseCase
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IPermissionRepository _permissionRepository;
        private readonly IRolePermissionRepository _rolePermissionRepository;
        private readonly IRequestContext _requestContext;
        private readonly IAuthorizationUnitOfWork _unitOfWork;
        private readonly IOutboxWriter _outboxWriter;
        private readonly IOutboxMessageIdGenerator _outboxMessageIdGenerator;

        public RevokePermissionFromRoleUseCase(
            IRoleRepository roleRepository,
            IPermissionRepository permissionRepository,
            IRolePermissionRepository rolePermissionRepository,
            IRequestContext requestContext,
            IAuthorizationUnitOfWork unitOfWork,
            IOutboxWriter outboxWriter,
            IOutboxMessageIdGenerator outboxMessageIdGenerator)
        {
            _roleRepository = roleRepository;
            _permissionRepository = permissionRepository;
            _rolePermissionRepository = rolePermissionRepository;
            _requestContext = requestContext;
            _unitOfWork = unitOfWork;
            _outboxWriter = outboxWriter;
            _outboxMessageIdGenerator = outboxMessageIdGenerator;
        }

        public async Task<RevokePermissionFromRoleResponseDto> ExecuteAsync(
            RevokePermissionFromRoleRequestDto request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.RoleId <= 0)
                throw new ArgumentOutOfRangeException(nameof(request.RoleId), "RoleId must be greater than zero.");

            if (request.PermissionId <= 0)
                throw new ArgumentOutOfRangeException(nameof(request.PermissionId), "PermissionId must be greater than zero.");

            var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
            if (role is null)
                throw new InvalidOperationException($"Role with id {request.RoleId} was not found.");

            var permission = await _permissionRepository.GetByIdAsync(request.PermissionId, cancellationToken);
            if (permission is null)
                throw new InvalidOperationException($"Permission with id {request.PermissionId} was not found.");

            var existingGrant = await _rolePermissionRepository.GetActiveByRoleIdAndPermissionIdAsync(
                request.RoleId,
                request.PermissionId,
                cancellationToken);

            if (existingGrant is null)
            {
                return new RevokePermissionFromRoleResponseDto
                {
                    RoleId = request.RoleId,
                    PermissionId = request.PermissionId,
                    IsRevoked = true,
                    WasAlreadyRevoked = true
                };
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var now = DateTime.UtcNow;
                var actorUserId = _requestContext.CurrentUserId;

                await _rolePermissionRepository.RevokeAsync(
                    request.RoleId,
                    request.PermissionId,
                    actorUserId,
                    cancellationToken);

                var integrationEvent = new RolePermissionRevokedEvent
                {
                    RolePermissionId = existingGrant.RolePermissionId,
                    RoleId = request.RoleId,
                    PermissionId = request.PermissionId,
                    ActorUserId = actorUserId,
                    OccurredAtUtc = now,
                    CorrelationId = _requestContext.CorrelationId
                };

                await _outboxWriter.WriteAsync(
                    messageId: _outboxMessageIdGenerator.NewId(),
                    eventType: AuthorizationOutboxConstants.EventTypes.RolePermissionRevoked,
                    aggregateType: AuthorizationOutboxConstants.AggregateTypes.RolePermission,
                    aggregateId: existingGrant.RolePermissionId.ToString(),
                    aggregatePublicId: null,
                    aggregateVersion: null,
                    payload: JsonSerializer.Serialize(integrationEvent),
                    headers: null,
                    correlationId: _requestContext.CorrelationId,
                    initiatorUserId: actorUserId,
                    occurredAtUtc: now,
                    cancellationToken: cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return new RevokePermissionFromRoleResponseDto
                {
                    RoleId = request.RoleId,
                    PermissionId = request.PermissionId,
                    IsRevoked = true,
                    WasAlreadyRevoked = false
                };
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}