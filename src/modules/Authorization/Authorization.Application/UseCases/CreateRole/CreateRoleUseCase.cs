using Authorization.Application.Contracts.Ports;
using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using Authorization.Application.Helpers;
using Authorization.Domain.Entities;

namespace Authorization.Application.UseCases.CreateRole
{
    public sealed class CreateRoleUseCase : ICreateRoleUseCase
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IRequestContext _requestContext;
        private readonly IPublicIdGenerator _publicIdGenerator;

        public CreateRoleUseCase(
            IRoleRepository roleRepository,
            IRequestContext requestContext,
            IPublicIdGenerator publicIdGenerator)
        {
            _roleRepository = roleRepository;
            _requestContext = requestContext;
            _publicIdGenerator = publicIdGenerator;
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

            var now = DateTime.UtcNow;
            var role = Role.CreateNew(
                publicId: _publicIdGenerator.NewId(),
                name: request.Name.Trim(),
                nameNormalized: normalizedName,
                description: request.Description,
                isSystem: request.IsSystem,
                createdAt: now,
                createdByUserId: _requestContext.CurrentUserId);

            var createdRole = await _roleRepository.InsertAsync(
                role,
                cancellationToken);

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
    }
}