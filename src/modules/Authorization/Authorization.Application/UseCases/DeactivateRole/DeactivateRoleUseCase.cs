using Authorization.Application.Contracts.Ports;
using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;

namespace Authorization.Application.UseCases.DeactivateRole
{
    public sealed class DeactivateRoleUseCase : IDeactivateRoleUseCase
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IRequestContext _requestContext;

        public DeactivateRoleUseCase(
            IRoleRepository roleRepository,
            IRequestContext requestContext)
        {
            _roleRepository = roleRepository;
            _requestContext = requestContext;
        }

        public async Task<DeactivateRoleResponseDto> ExecuteAsync(
            DeactivateRoleRequestDto request,
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

            if (!role.IsActive)
            {
                return new DeactivateRoleResponseDto
                {
                    RoleId = request.RoleId,
                    IsDeactivated = true,
                    WasAlreadyDeactivated = true
                };
            }

            role.Deactivate(
                DateTime.UtcNow,
                _requestContext.CurrentUserId);

            await _roleRepository.UpdateAsync(role, cancellationToken);

            return new DeactivateRoleResponseDto
            {
                RoleId = request.RoleId,
                IsDeactivated = true,
                WasAlreadyDeactivated = false
            };
        }
    }
}