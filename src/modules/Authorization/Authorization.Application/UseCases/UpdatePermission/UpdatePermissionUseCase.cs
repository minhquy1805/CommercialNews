using System.Text.Json;
using Authorization.Application.Contracts.Events;
using Authorization.Application.Contracts.Ports;
using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using Authorization.Application.Helpers;
using CommercialNews.BuildingBlocks.Messaging.Outbox;

namespace Authorization.Application.UseCases.UpdatePermission
{
    public sealed class UpdatePermissionUseCase : IUpdatePermissionUseCase
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly IRequestContext _requestContext;
        private readonly IOutboxMessageIdGenerator _outboxMessageIdGenerator;
        private readonly IAuthorizationUnitOfWork _unitOfWork;
        private readonly IOutboxWriter _outboxWriter;

        public UpdatePermissionUseCase(
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

        public async Task<UpdatePermissionResponseDto> ExecuteAsync(
            UpdatePermissionRequestDto request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.PermissionId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(request.PermissionId), "PermissionId must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Permission name is required.", nameof(request.Name));
            }

            var permission = await _permissionRepository.GetByIdAsync(
                request.PermissionId,
                cancellationToken);

            if (permission is null)
            {
                throw new InvalidOperationException($"Permission with id {request.PermissionId} was not found.");
            }

            var normalizedName = AuthorizationNameNormalizer.Normalize(request.Name);

            var existingPermission = await _permissionRepository.GetByNameNormalizedAsync(
                normalizedName,
                cancellationToken);

            if (existingPermission is not null && existingPermission.PermissionId != permission.PermissionId)
            {
                throw new InvalidOperationException(
                    $"Another permission with normalized name '{normalizedName}' already exists.");
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var now = DateTime.UtcNow;
                var actorUserId = _requestContext.CurrentUserId;

                permission.Rename(
                    request.Name.Trim(),
                    normalizedName,
                    now,
                    actorUserId);

                permission.ChangeDescription(
                    request.Description,
                    now,
                    actorUserId);

                permission.ChangeModule(
                    request.Module,
                    now,
                    actorUserId);

                var updatedPermission = await _permissionRepository.UpdateAsync(
                    permission,
                    cancellationToken);

                var integrationEvent = new PermissionUpdatedEvent
                {
                    PermissionId = updatedPermission.PermissionId,
                    PublicId = updatedPermission.PublicId,
                    Name = updatedPermission.Name,
                    NameNormalized = updatedPermission.NameNormalized,
                    Description = updatedPermission.Description,
                    Module = updatedPermission.Module,
                    IsSystem = updatedPermission.IsSystem,
                    IsActive = updatedPermission.IsActive,
                    ActorUserId = actorUserId,
                    OccurredAtUtc = now,
                    CorrelationId = _requestContext.CorrelationId
                };

                await _outboxWriter.WriteAsync(
                    messageId: _outboxMessageIdGenerator.NewId(),
                    eventType: AuthorizationOutboxConstants.EventTypes.PermissionUpdated,
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

                return new UpdatePermissionResponseDto
                {
                    PermissionId = updatedPermission.PermissionId,
                    PublicId = updatedPermission.PublicId,
                    Name = updatedPermission.Name,
                    NameNormalized = updatedPermission.NameNormalized,
                    Description = updatedPermission.Description,
                    Module = updatedPermission.Module,
                    IsSystem = updatedPermission.IsSystem,
                    IsActive = updatedPermission.IsActive,
                    UpdatedAt = updatedPermission.UpdatedAt,
                    UpdatedByUserId = updatedPermission.UpdatedByUserId
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