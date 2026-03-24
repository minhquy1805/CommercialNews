using Authorization.Application.Contracts.Ports;
using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using Authorization.Application.Helpers;

namespace Authorization.Application.UseCases.CheckUserHasPermission
{
    public sealed class CheckUserHasPermissionUseCase : ICheckUserHasPermissionUseCase
    {
        private readonly IAuthorizationUserLookupService _authorizationUserLookupService;
        private readonly IAuthorizationPermissionQueryRepository _authorizationPermissionQueryRepository;

        public CheckUserHasPermissionUseCase(
            IAuthorizationUserLookupService authorizationUserLookupService,
            IAuthorizationPermissionQueryRepository authorizationPermissionQueryRepository)
        {
            _authorizationUserLookupService = authorizationUserLookupService;
            _authorizationPermissionQueryRepository = authorizationPermissionQueryRepository;
        }

        public async Task<CheckUserHasPermissionResponseDto> ExecuteAsync(
            CheckUserHasPermissionRequestDto request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.UserId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(request.UserId), "UserId must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(request.PermissionName))
            {
                throw new ArgumentException("Permission name is required.", nameof(request.PermissionName));
            }

            var userExists = await _authorizationUserLookupService.ExistsAsync(
                request.UserId,
                cancellationToken);

            if (!userExists)
            {
                throw new InvalidOperationException($"User with id {request.UserId} was not found.");
            }

            var normalizedPermissionName = AuthorizationNameNormalizer.Normalize(request.PermissionName);

            var permissions = await _authorizationPermissionQueryRepository.GetEffectivePermissionsByUserIdAsync(
                request.UserId,
                cancellationToken);

            var hasPermission = permissions.Any(x => x.NameNormalized == normalizedPermissionName);

            return new CheckUserHasPermissionResponseDto
            {
                UserId = request.UserId,
                PermissionName = request.PermissionName.Trim(),
                PermissionNameNormalized = normalizedPermissionName,
                HasPermission = hasPermission
            };
        }
    }
}