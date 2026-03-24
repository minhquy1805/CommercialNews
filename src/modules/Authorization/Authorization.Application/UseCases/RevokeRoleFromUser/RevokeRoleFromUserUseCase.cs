using Authorization.Application.Contracts.Ports;
using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;

namespace Authorization.Application.UseCases.RevokeRoleFromUser
{
    public sealed class RevokeRoleFromUserUseCase : IRevokeRoleFromUserUseCase
    {
        private readonly IAuthorizationUserLookupService _authorizationUserLookupService;
        private readonly IRoleRepository _roleRepository;
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly IRequestContext _requestContext;

        public RevokeRoleFromUserUseCase(
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

            await _userRoleRepository.RevokeAsync(
                request.UserId,
                request.RoleId,
                _requestContext.CurrentUserId,
                cancellationToken);

            return new RevokeRoleFromUserResponseDto
            {
                UserId = request.UserId,
                RoleId = request.RoleId,
                IsRevoked = true,
                WasAlreadyRevoked = false
            };
        }
    }
}