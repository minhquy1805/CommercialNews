using System.Text.Json;
using Authorization.Application.Contracts.Events;
using Authorization.Application.Contracts.Ports;
using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using Authorization.Application.Helpers;
using Authorization.Domain.Entities;
using CommercialNews.BuildingBlocks.Messaging.Outbox;

namespace Authorization.Application.UseCases.CreatePermission
{
    public sealed class CreatePermissionUseCase : ICreatePermissionUseCase
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly IRequestContext _requestContext;
        private readonly IPublicIdGenerator _publicIdGenerator;
        private readonly IOutboxMessageIdGenerator _outboxMessageIdGenerator;
        private readonly IAuthorizationUnitOfWork _unitOfWork;
        private readonly IOutboxWriter _outboxWriter;

        public CreatePermissionUseCase(
            IPermissionRepository permissionRepository,
            IRequestContext requestContext,
            IPublicIdGenerator publicIdGenerator,
            IOutboxMessageIdGenerator outboxMessageIdGenerator,
            IAuthorizationUnitOfWork unitOfWork,
            IOutboxWriter outboxWriter)
        {
            _permissionRepository = permissionRepository;
            _requestContext = requestContext;
            _publicIdGenerator = publicIdGenerator;
            _outboxMessageIdGenerator = outboxMessageIdGenerator;
            _unitOfWork = unitOfWork;
            _outboxWriter = outboxWriter;
        }

        public async Task<CreatePermissionResponseDto> ExecuteAsync(
            CreatePermissionRequestDto request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Permission name is required.", nameof(request.Name));
            }

            var normalizedName = AuthorizationNameNormalizer.Normalize(request.Name);

            var existingPermission = await _permissionRepository.GetByNameNormalizedAsync(
                normalizedName,
                cancellationToken);

            if (existingPermission is not null)
            {
                throw new InvalidOperationException(
                    $"Permission with normalized name '{normalizedName}' already exists.");
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var now = DateTime.UtcNow;
                var actorUserId = _requestContext.CurrentUserId;

                var permission = Permission.CreateNew(
                    publicId: _publicIdGenerator.NewId(),
                    name: request.Name.Trim(),
                    nameNormalized: normalizedName,
                    description: request.Description,
                    module: request.Module,
                    isSystem: request.IsSystem,
                    createdAt: now,
                    createdByUserId: actorUserId);

                var createdPermission = await _permissionRepository.InsertAsync(
                    permission,
                    cancellationToken);

                var integrationEvent = new PermissionCreatedEvent
                {
                    PermissionId = createdPermission.PermissionId,
                    PublicId = createdPermission.PublicId,
                    Name = createdPermission.Name,
                    NameNormalized = createdPermission.NameNormalized,
                    Module = createdPermission.Module,
                    IsSystem = createdPermission.IsSystem,
                    IsActive = createdPermission.IsActive,
                    ActorUserId = actorUserId,
                    OccurredAtUtc = now,
                    CorrelationId = _requestContext.CorrelationId
                };

                await _outboxWriter.WriteAsync(
                    messageId: _outboxMessageIdGenerator.NewId(),
                    eventType: AuthorizationOutboxConstants.EventTypes.PermissionCreated,
                    aggregateType: AuthorizationOutboxConstants.AggregateTypes.Permission,
                    aggregateId: createdPermission.PermissionId.ToString(),
                    aggregatePublicId: createdPermission.PublicId,
                    aggregateVersion: null,
                    payload: JsonSerializer.Serialize(integrationEvent),
                    headers: null,
                    correlationId: _requestContext.CorrelationId,
                    initiatorUserId: actorUserId,
                    occurredAtUtc: now,
                    cancellationToken: cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return new CreatePermissionResponseDto
                {
                    PermissionId = createdPermission.PermissionId,
                    PublicId = createdPermission.PublicId,
                    Name = createdPermission.Name,
                    NameNormalized = createdPermission.NameNormalized,
                    Description = createdPermission.Description,
                    Module = createdPermission.Module,
                    IsSystem = createdPermission.IsSystem,
                    IsActive = createdPermission.IsActive,
                    CreatedAt = createdPermission.CreatedAt,
                    CreatedByUserId = createdPermission.CreatedByUserId
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