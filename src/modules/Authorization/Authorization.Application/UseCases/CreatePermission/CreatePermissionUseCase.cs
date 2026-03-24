using Authorization.Application.Contracts.Ports;
using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using Authorization.Application.Helpers;
using Authorization.Domain.Entities;

namespace Authorization.Application.UseCases.CreatePermission
{
    public sealed class CreatePermissionUseCase : ICreatePermissionUseCase
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly IRequestContext _requestContext;
        private readonly IPublicIdGenerator _publicIdGenerator;

        public CreatePermissionUseCase(
            IPermissionRepository permissionRepository,
            IRequestContext requestContext,
            IPublicIdGenerator publicIdGenerator)
        {
            _permissionRepository = permissionRepository;
            _requestContext = requestContext;
            _publicIdGenerator = publicIdGenerator;
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

            var now = DateTime.UtcNow;
            var permission = Permission.CreateNew(
                publicId: _publicIdGenerator.NewId(),
                name: request.Name.Trim(),
                nameNormalized: normalizedName,
                description: request.Description,
                module: request.Module,
                isSystem: request.IsSystem,
                createdAt: now,
                createdByUserId: _requestContext.CurrentUserId);

            var createdPermission = await _permissionRepository.InsertAsync(
                permission,
                cancellationToken);

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
    }
}