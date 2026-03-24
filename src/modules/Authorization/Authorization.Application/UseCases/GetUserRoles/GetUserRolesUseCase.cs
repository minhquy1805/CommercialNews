using Authorization.Application.Contracts.Ports;
using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;

namespace Authorization.Application.UseCases.GetUserRoles
{
    public sealed class GetUserRolesUseCase : IGetUserRolesUseCase
    {
        private readonly IAuthorizationUserLookupService _authorizationUserLookupService;
        private readonly IUserRoleRepository _userRoleRepository;

        public GetUserRolesUseCase(
            IAuthorizationUserLookupService authorizationUserLookupService,
            IUserRoleRepository userRoleRepository)
        {
            _authorizationUserLookupService = authorizationUserLookupService;
            _userRoleRepository = userRoleRepository;
        }

        public async Task<GetUserRolesResponseDto> ExecuteAsync(
            GetUserRolesRequestDto request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.UserId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(request.UserId), "UserId must be greater than zero.");
            }

            var userExists = await _authorizationUserLookupService.ExistsAsync(
                request.UserId,
                cancellationToken);

            if (!userExists)
            {
                throw new InvalidOperationException($"User with id {request.UserId} was not found.");
            }

            var roles = await _userRoleRepository.GetActiveRolesByUserIdAsync(
                request.UserId,
                cancellationToken);

            return new GetUserRolesResponseDto
            {
                UserId = request.UserId,
                Roles = roles.Select(x => new UserRoleItemDto
                {
                    RoleId = x.RoleId,
                    PublicId = x.PublicId,
                    Name = x.Name,
                    NameNormalized = x.NameNormalized,
                    Description = x.Description,
                    IsSystem = x.IsSystem,
                    IsActive = x.IsActive,
                    AssignedAt = x.AssignedAt,
                    AssignedByUserId = x.AssignedByUserId
                }).ToList()
            };
        }
    }
}