using Authorization.Application.Contracts.Ports;
using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using Authorization.Domain.Entities;

namespace Authorization.Application.UseCases.AssignRoleToUser
{
    public sealed class AssignRoleToUserUseCase : IAssignRoleToUserUseCase
    {
        private readonly IAuthorizationUserLookupService _authorizationUserLookupService;
        private readonly IRoleRepository _roleRepository;
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly IRequestContext _requestContext;

        public AssignRoleToUserUseCase(
            IAuthorizationUserLookupService authorizationUserLookupService,
            IRoleRepository roleRepository,
            IUserRoleRepository userRoleRepository,
            IRequestContext requestContext)
        {
            _authorizationUserLookupService = authorizationUserLookupService;
            _roleRepository = roleRepository;
            _userRoleRepository = userRoleRepository;
            _requestContext = requestContext;
        }

        public async Task<AssignRoleToUserResponseDto> ExecuteAsync(
            AssignRoleToUserRequestDto request,
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

            if (!role.IsActive)
            {
                throw new InvalidOperationException($"Role with id {request.RoleId} is inactive.");
            }

            var existingAssignment = await _userRoleRepository.GetActiveByUserIdAndRoleIdAsync(
                request.UserId,
                request.RoleId,
                cancellationToken);

            if (existingAssignment is not null)
            {
                return new AssignRoleToUserResponseDto
                {
                    UserRoleId = existingAssignment.UserRoleId,
                    UserId = existingAssignment.UserId,
                    RoleId = existingAssignment.RoleId,
                    IsAssigned = true,
                    WasAlreadyAssigned = true
                };
            }

            var newAssignment = UserRole.CreateNew(
                userId: request.UserId,
                roleId: request.RoleId,
                assignedAt: DateTime.UtcNow,
                assignedByUserId: _requestContext.CurrentUserId);

            var assignedUserRole = await _userRoleRepository.InsertAsync(
                newAssignment,
                cancellationToken);

            return new AssignRoleToUserResponseDto
            {
                UserRoleId = assignedUserRole.UserRoleId,
                UserId = assignedUserRole.UserId,
                RoleId = assignedUserRole.RoleId,
                IsAssigned = true,
                WasAlreadyAssigned = false
            };
        }
    }
}