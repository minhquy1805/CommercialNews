using System.Text.Json;
using Authorization.Application.Contracts.Events;
using Authorization.Application.Contracts.Ports;
using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using CommercialNews.BuildingBlocks.Messaging.Outbox;

namespace Authorization.Application.UseCases.ActivatePermission
{
    public sealed class ActivatePermissionUseCase : IActivatePermissionUseCase
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly IRequestContext _requestContext;
        private readonly IOutboxMessageIdGenerator _outboxMessageIdGenerator;
        private readonly IAuthorizationUnitOfWork _unitOfWork;
        private readonly IOutboxWriter _outboxWriter;

        public ActivatePermissionUseCase(
            IPermissionRepository permissionRepository,
            IRequestContext requestContext,
            IOutboxMessageIdGenerator outboxMessageIdGenerator,
            IAuthorizationUnitOfWork unitOfWork,
            IOutboxWriter outboxWriter)
        {
            _permissionRepository = permissionRepository;
            _requestContext = requestContext;
            _outboxMessageIdGenerator = outboxMessageIdGenerator;
            _unitOfWork = unitOfWork;
            _outboxWriter = outboxWriter;
        }

        public async Task<ActivatePermissionResponseDto> ExecuteAsync(
            ActivatePermissionRequestDto request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.PermissionId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(request.PermissionId), "PermissionId must be greater than zero.");
            }

            var permission = await _permissionRepository.GetByIdAsync(request.PermissionId, cancellationToken);

            if (permission is null)
            {
                throw new InvalidOperationException($"Permission with id {request.PermissionId} was not found.");
            }

            if (permission.IsActive)
            {
                return new ActivatePermissionResponseDto
                {
                    PermissionId = request.PermissionId,
                    IsActivated = true,
                    WasAlreadyActivated = true
                };
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var now = DateTime.UtcNow;
                var actorUserId = _requestContext.CurrentUserId;

                permission.Activate(now, actorUserId);

                var updatedPermission = await _permissionRepository.UpdateAsync(permission, cancellationToken);

                var integrationEvent = new PermissionActivatedEvent
                {
                    PermissionId = updatedPermission.PermissionId,
                    PublicId = updatedPermission.PublicId,
                    Name = updatedPermission.Name,
                    ActorUserId = actorUserId,
                    OccurredAtUtc = now,
                    CorrelationId = _requestContext.CorrelationId
                };

                await _outboxWriter.WriteAsync(
                    messageId: _outboxMessageIdGenerator.NewId(),
                    eventType: AuthorizationOutboxConstants.EventTypes.PermissionActivated,
                    aggregateType: AuthorizationOutboxConstants.AggregateTypes.Permission,
                    aggregateId: updatedPermission.PermissionId.ToString(),
                    aggregatePublicId: updatedPermission.PublicId,
                    aggregateVersion: null,
                    payload: JsonSerializer.Serialize(integrationEvent),
                    headers: null,
                    correlationId: _requestContext.CorrelationId,
                    initiatorUserId: actorUserId,
                    occurredAtUtc: now,
                    cancellationToken: cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return new ActivatePermissionResponseDto
                {
                    PermissionId = request.PermissionId,
                    IsActivated = true,
                    WasAlreadyActivated = false
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