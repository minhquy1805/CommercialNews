using System.Text.Json;
using Authorization.Application.Contracts.Events;
using Authorization.Application.Contracts.Ports;
using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using Authorization.Application.Helpers;
using CommercialNews.BuildingBlocks.Messaging.Outbox;

namespace Authorization.Application.UseCases.UpdateRole
{
    public sealed class UpdateRoleUseCase : IUpdateRoleUseCase
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IRequestContext _requestContext;
        private readonly IOutboxMessageIdGenerator _outboxMessageIdGenerator;
        private readonly IAuthorizationUnitOfWork _unitOfWork;
        private readonly IOutboxWriter _outboxWriter;

        public UpdateRoleUseCase(
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

        public async Task<UpdateRoleResponseDto> ExecuteAsync(
            UpdateRoleRequestDto request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.RoleId <= 0)
                throw new ArgumentOutOfRangeException(nameof(request.RoleId), "RoleId must be greater than zero.");

            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Role name is required.", nameof(request.Name));

            var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
            if (role is null)
                throw new InvalidOperationException($"Role with id {request.RoleId} was not found.");

            var normalizedName = AuthorizationNameNormalizer.Normalize(request.Name);

            var existingRole = await _roleRepository.GetByNameNormalizedAsync(
                normalizedName,
                cancellationToken);

            if (existingRole is not null && existingRole.RoleId != role.RoleId)
            {
                throw new InvalidOperationException(
                    $"Another role with normalized name '{normalizedName}' already exists.");
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var now = DateTime.UtcNow;
                var actorUserId = _requestContext.CurrentUserId;

                role.Rename(request.Name.Trim(), normalizedName, now, actorUserId);
                role.ChangeDescription(request.Description, now, actorUserId);

                var updatedRole = await _roleRepository.UpdateAsync(role, cancellationToken);

                var integrationEvent = new RoleUpdatedEvent
                {
                    RoleId = updatedRole.RoleId,
                    PublicId = updatedRole.PublicId,
                    Name = updatedRole.Name,
                    NameNormalized = updatedRole.NameNormalized,
                    Description = updatedRole.Description,
                    IsSystem = updatedRole.IsSystem,
                    IsActive = updatedRole.IsActive,
                    ActorUserId = actorUserId,
                    OccurredAtUtc = now,
                    CorrelationId = _requestContext.CorrelationId
                };

                await _outboxWriter.WriteAsync(
                    messageId: _outboxMessageIdGenerator.NewId(),
                    eventType: AuthorizationOutboxConstants.EventTypes.RoleUpdated,
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

                return new UpdateRoleResponseDto
                {
                    RoleId = updatedRole.RoleId,
                    PublicId = updatedRole.PublicId,
                    Name = updatedRole.Name,
                    NameNormalized = updatedRole.NameNormalized,
                    Description = updatedRole.Description,
                    IsSystem = updatedRole.IsSystem,
                    IsActive = updatedRole.IsActive,
                    UpdatedAt = updatedRole.UpdatedAt,
                    UpdatedByUserId = updatedRole.UpdatedByUserId
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