using System.Text.Json;
using Authorization.Application.Contracts.Events;
using Authorization.Application.Contracts.Ports;
using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using CommercialNews.BuildingBlocks.Messaging.Outbox;

namespace Authorization.Application.UseCases.RevokeRoleFromUser
{
    public sealed class RevokeRoleFromUserUseCase : IRevokeRoleFromUserUseCase
    {
        private readonly IAuthorizationUserLookupService _authorizationUserLookupService;
        private readonly IRoleRepository _roleRepository;
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly IRequestContext _requestContext;
        private readonly IAuthorizationUnitOfWork _unitOfWork;
        private readonly IOutboxWriter _outboxWriter;
        private readonly IOutboxMessageIdGenerator _outboxMessageIdGenerator;

        public RevokeRoleFromUserUseCase(
            IAuthorizationUserLookupService authorizationUserLookupService,
            IRoleRepository roleRepository,
            IUserRoleRepository userRoleRepository,
            IRequestContext requestContext,
            IAuthorizationUnitOfWork unitOfWork,
            IOutboxWriter outboxWriter,
            IOutboxMessageIdGenerator outboxMessageIdGenerator)
        {
            _authorizationUserLookupService = authorizationUserLookupService;
            _roleRepository = roleRepository;
            _userRoleRepository = userRoleRepository;
            _requestContext = requestContext;
            _unitOfWork = unitOfWork;
            _outboxWriter = outboxWriter;
            _outboxMessageIdGenerator = outboxMessageIdGenerator;
        }

        public async Task<RevokeRoleFromUserResponseDto> ExecuteAsync(
            RevokeRoleFromUserRequestDto request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.UserId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(request.UserId), "UserId must be greater than zero.");
            }

            if (request.RoleId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(request.RoleId), "RoleId must be greater than zero.");
            }

            var userExists = await _authorizationUserLookupService.ExistsAsync(
                request.UserId,
                cancellationToken);

            if (!userExists)
            {
                throw new InvalidOperationException($"User with id {request.UserId} was not found.");
            }

            var role = await _roleRepository.GetByIdAsync(
                request.RoleId,
                cancellationToken);

            if (role is null)
            {
                throw new InvalidOperationException($"Role with id {request.RoleId} was not found.");
            }

            var existingAssignment = await _userRoleRepository.GetActiveByUserIdAndRoleIdAsync(
                request.UserId,
                request.RoleId,
                cancellationToken);

            if (existingAssignment is null)
            {
                return new RevokeRoleFromUserResponseDto
                {
                    UserId = request.UserId,
                    RoleId = request.RoleId,
                    IsRevoked = true,
                    WasAlreadyRevoked = true
                };
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var now = DateTime.UtcNow;
                var actorUserId = _requestContext.CurrentUserId;

                await _userRoleRepository.RevokeAsync(
                    request.UserId,
                    request.RoleId,
                    actorUserId,
                    cancellationToken);

                var integrationEvent = new UserRoleRevokedEvent
                {
                    UserRoleId = existingAssignment.UserRoleId,
                    TargetUserId = request.UserId,
                    RoleId = request.RoleId,
                    ActorUserId = actorUserId,
                    OccurredAtUtc = now,
                    CorrelationId = _requestContext.CorrelationId
                };

                await _outboxWriter.WriteAsync(
                    messageId: _outboxMessageIdGenerator.NewId(),
                    eventType: AuthorizationOutboxConstants.EventTypes.UserRoleRevoked,
                    aggregateType: AuthorizationOutboxConstants.AggregateTypes.UserRole,
                    aggregateId: existingAssignment.UserRoleId.ToString(),
                    aggregatePublicId: null,
                    aggregateVersion: null,
                    payload: JsonSerializer.Serialize(integrationEvent),
                    headers: null,
                    correlationId: _requestContext.CorrelationId,
                    initiatorUserId: actorUserId,
                    occurredAtUtc: now,
                    cancellationToken: cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return new RevokeRoleFromUserResponseDto
                {
                    UserId = request.UserId,
                    RoleId = request.RoleId,
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