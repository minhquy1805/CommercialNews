using System.Text.Json;
using Authorization.Application.Contracts.Events;
using Authorization.Application.Contracts.Ports;
using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using Authorization.Application.Helpers;
using Authorization.Domain.Entities;
using CommercialNews.BuildingBlocks.Messaging.Outbox;

namespace Authorization.Application.UseCases.CreateRole
{
    public sealed class CreateRoleUseCase : ICreateRoleUseCase
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IRequestContext _requestContext;
        private readonly IPublicIdGenerator _publicIdGenerator;
        private readonly IOutboxMessageIdGenerator _outboxMessageIdGenerator;
        private readonly IAuthorizationUnitOfWork _unitOfWork;
        private readonly IOutboxWriter _outboxWriter;

        public CreateRoleUseCase(
            IRoleRepository roleRepository,
            IRequestContext requestContext,
            IPublicIdGenerator publicIdGenerator,
            IOutboxMessageIdGenerator outboxMessageIdGenerator,
            IAuthorizationUnitOfWork unitOfWork,
            IOutboxWriter outboxWriter)
        {
            _roleRepository = roleRepository;
            _requestContext = requestContext;
            _publicIdGenerator = publicIdGenerator;
            _outboxMessageIdGenerator = outboxMessageIdGenerator;
            _unitOfWork = unitOfWork;
            _outboxWriter = outboxWriter;
        }

        public async Task<CreateRoleResponseDto> ExecuteAsync(
            CreateRoleRequestDto request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Role name is required.", nameof(request.Name));
            }

            var normalizedName = AuthorizationNameNormalizer.Normalize(request.Name);

            var existingRole = await _roleRepository.GetByNameNormalizedAsync(
                normalizedName,
                cancellationToken);

            if (existingRole is not null)
            {
                throw new InvalidOperationException(
                    $"Role with normalized name '{normalizedName}' already exists.");
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var now = DateTime.UtcNow;
                var actorUserId = _requestContext.CurrentUserId;

                var role = Role.CreateNew(
                    publicId: _publicIdGenerator.NewId(),
                    name: request.Name.Trim(),
                    nameNormalized: normalizedName,
                    description: request.Description,
                    isSystem: request.IsSystem,
                    createdAt: now,
                    createdByUserId: actorUserId);

                var createdRole = await _roleRepository.InsertAsync(
                    role,
                    cancellationToken);

                var integrationEvent = new RoleCreatedEvent
                {
                    RoleId = createdRole.RoleId,
                    PublicId = createdRole.PublicId,
                    Name = createdRole.Name,
                    NameNormalized = createdRole.NameNormalized,
                    IsSystem = createdRole.IsSystem,
                    IsActive = createdRole.IsActive,
                    ActorUserId = actorUserId,
                    OccurredAtUtc = now,
                    CorrelationId = _requestContext.CorrelationId
                };

                await _outboxWriter.WriteAsync(
                    messageId: _outboxMessageIdGenerator.NewId(),
                    eventType: AuthorizationOutboxConstants.EventTypes.RoleCreated,
                    aggregateType: AuthorizationOutboxConstants.AggregateTypes.Role,
                    aggregateId: createdRole.RoleId.ToString(),
                    aggregatePublicId: createdRole.PublicId,
                    aggregateVersion: null,
                    payload: JsonSerializer.Serialize(integrationEvent),
                    headers: null,
                    correlationId: _requestContext.CorrelationId,
                    initiatorUserId: actorUserId,
                    occurredAtUtc: now,
                    cancellationToken: cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return new CreateRoleResponseDto
                {
                    RoleId = createdRole.RoleId,
                    PublicId = createdRole.PublicId,
                    Name = createdRole.Name,
                    NameNormalized = createdRole.NameNormalized,
                    Description = createdRole.Description,
                    IsSystem = createdRole.IsSystem,
                    IsActive = createdRole.IsActive,
                    CreatedAt = createdRole.CreatedAt,
                    CreatedByUserId = createdRole.CreatedByUserId
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