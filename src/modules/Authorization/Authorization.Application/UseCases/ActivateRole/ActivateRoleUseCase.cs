using System.Text.Json;
using Authorization.Application.Contracts.Events;
using Authorization.Application.Contracts.Ports;
using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using CommercialNews.BuildingBlocks.Messaging.Outbox;

namespace Authorization.Application.UseCases.ActivateRole
{
    public sealed class ActivateRoleUseCase : IActivateRoleUseCase
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IRequestContext _requestContext;
        private readonly IOutboxMessageIdGenerator _outboxMessageIdGenerator;
        private readonly IAuthorizationUnitOfWork _unitOfWork;
        private readonly IOutboxWriter _outboxWriter;

        public ActivateRoleUseCase(
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

        public async Task<ActivateRoleResponseDto> ExecuteAsync(
            ActivateRoleRequestDto request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.RoleId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(request.RoleId), "RoleId must be greater than zero.");
            }

            var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);

            if (role is null)
            {
                throw new InvalidOperationException($"Role with id {request.RoleId} was not found.");
            }

            if (role.IsActive)
            {
                return new ActivateRoleResponseDto
                {
                    RoleId = request.RoleId,
                    IsActivated = true,
                    WasAlreadyActivated = true
                };
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var now = DateTime.UtcNow;
                var actorUserId = _requestContext.CurrentUserId;

                role.Activate(now, actorUserId);

                var updatedRole = await _roleRepository.UpdateAsync(role, cancellationToken);

                var integrationEvent = new RoleActivatedEvent
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
                    eventType: AuthorizationOutboxConstants.EventTypes.RoleActivated,
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

                return new ActivateRoleResponseDto
                {
                    RoleId = request.RoleId,
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