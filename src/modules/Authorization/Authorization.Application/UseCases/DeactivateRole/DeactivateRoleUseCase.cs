using System.Text.Json;
using Authorization.Application.Contracts.Events;
using Authorization.Application.Contracts.Ports;
using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using CommercialNews.BuildingBlocks.Messaging.Outbox;

namespace Authorization.Application.UseCases.DeactivateRole
{
    public sealed class DeactivateRoleUseCase : IDeactivateRoleUseCase
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IRequestContext _requestContext;
        private readonly IOutboxMessageIdGenerator _outboxMessageIdGenerator;
        private readonly IAuthorizationUnitOfWork _unitOfWork;
        private readonly IOutboxWriter _outboxWriter;

        public DeactivateRoleUseCase(
            IRoleRepository roleRepository,
            IRequestContext requestContext,
            IOutboxMessageIdGenerator outboxMessageIdGenerator,
            IAuthorizationUnitOfWork unitOfWork,
            IOutboxWriter outboxWriter)
        {
            _roleRepository = roleRepository;
            _requestContext = requestContext;
            _outboxMessageIdGenerator = outboxMessageIdGenerator;
            _unitOfWork = unitOfWork;
            _outboxWriter = outboxWriter;
        }

        public async Task<DeactivateRoleResponseDto> ExecuteAsync(
            DeactivateRoleRequestDto request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.RoleId <= 0)
                throw new ArgumentOutOfRangeException(nameof(request.RoleId), "RoleId must be greater than zero.");

            var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
            if (role is null)
                throw new InvalidOperationException($"Role with id {request.RoleId} was not found.");

            if (!role.IsActive)
            {
                return new DeactivateRoleResponseDto
                {
                    RoleId = request.RoleId,
                    IsDeactivated = true,
                    WasAlreadyDeactivated = true
                };
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var now = DateTime.UtcNow;
                var actorUserId = _requestContext.CurrentUserId;

                role.Deactivate(now, actorUserId);
                var updatedRole = await _roleRepository.UpdateAsync(role, cancellationToken);

                var integrationEvent = new RoleDeactivatedEvent
                {
                    RoleId = updatedRole.RoleId,
                    PublicId = updatedRole.PublicId,
                    Name = updatedRole.Name,
                    ActorUserId = actorUserId,
                    OccurredAtUtc = now,
                    CorrelationId = _requestContext.CorrelationId
                };

                await _outboxWriter.WriteAsync(
                    messageId: _outboxMessageIdGenerator.NewId(),
                    eventType: AuthorizationOutboxConstants.EventTypes.RoleDeactivated,
                    aggregateType: AuthorizationOutboxConstants.AggregateTypes.Role,
                    aggregateId: updatedRole.RoleId.ToString(),
                    aggregatePublicId: updatedRole.PublicId,
                    aggregateVersion: null,
                    payload: JsonSerializer.Serialize(integrationEvent),
                    headers: null,
                    correlationId: _requestContext.CorrelationId,
                    initiatorUserId: actorUserId,
                    occurredAtUtc: now,
                    cancellationToken: cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return new DeactivateRoleResponseDto
                {
                    RoleId = request.RoleId,
                    IsDeactivated = true,
                    WasAlreadyDeactivated = false
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