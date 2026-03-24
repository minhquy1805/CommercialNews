using Authorization.Application.Contracts.Ports;
using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;

namespace Authorization.Application.UseCases.GetRoleUsers
{
    public sealed class GetRoleUsersUseCase : IGetRoleUsersUseCase
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IUserRoleRepository _userRoleRepository;

        public GetRoleUsersUseCase(
            IRoleRepository roleRepository,
            IUserRoleRepository userRoleRepository)
        {
            _roleRepository = roleRepository;
            _userRoleRepository = userRoleRepository;
        }

        public async Task<GetRoleUsersResponseDto> ExecuteAsync(
            GetRoleUsersRequestDto request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.RoleId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(request.RoleId), "RoleId must be greater than zero.");
            }

            var role = await _roleRepository.GetByIdAsync(
                request.RoleId,
                cancellationToken);

            if (role is null)
            {
                throw new InvalidOperationException($"Role with id {request.RoleId} was not found.");
            }

            var users = await _userRoleRepository.GetActiveUsersByRoleIdAsync(
                request.RoleId,
                cancellationToken);

            return new GetRoleUsersResponseDto
            {
                RoleId = request.RoleId,
                Users = users.Select(x => new RoleUserItemDto
                {
                    UserId = x.UserId,
                    PublicId = x.PublicId,
                    Email = x.Email,
                    FullName = x.FullName,
                    Status = x.Status,
                    IsEmailVerified = x.IsEmailVerified,
                    AssignedAt = x.AssignedAt,
                    AssignedByUserId = x.AssignedByUserId
                }).ToList()
            };
        }
    }
}