using Authorization.Application.Contracts.Ports;
using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;

namespace Authorization.Application.UseCases.GetUserEffectivePermissions
{
    public sealed class GetUserEffectivePermissionsUseCase : IGetUserEffectivePermissionsUseCase
    {
        private readonly IAuthorizationUserLookupService _authorizationUserLookupService;
        private readonly IAuthorizationPermissionQueryRepository _authorizationPermissionQueryRepository;

        public GetUserEffectivePermissionsUseCase(
            IAuthorizationUserLookupService authorizationUserLookupService,
            IAuthorizationPermissionQueryRepository authorizationPermissionQueryRepository)
        {
            _authorizationUserLookupService = authorizationUserLookupService;
            _authorizationPermissionQueryRepository = authorizationPermissionQueryRepository;
        }

        public async Task<GetUserEffectivePermissionsResponseDto> ExecuteAsync(
            GetUserEffectivePermissionsRequestDto request,
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

            var permissions = await _authorizationPermissionQueryRepository.GetEffectivePermissionsByUserIdAsync(
                request.UserId,
                cancellationToken);

            return new GetUserEffectivePermissionsResponseDto
            {
                UserId = request.UserId,
                Permissions = permissions.Select(x => new EffectivePermissionItemDto
                {
                    PermissionId = x.PermissionId,
                    PublicId = x.PublicId,
                    Name = x.Name,
                    NameNormalized = x.NameNormalized,
                    Description = x.Description,
                    Module = x.Module,
                    IsSystem = x.IsSystem,
                    IsActive = x.IsActive
                }).ToList()
            };
        }
    }
}