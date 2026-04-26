using Authorization.Application.Ports.Persistence;
using Authorization.Domain.Constants;
using Authorization.Domain.Entities;
using CommercialNews.BuildingBlocks.SharedKernel.Time;

namespace Authorization.Infrastructure.Seeding;

public sealed class RolePermissionSeederService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IRolePermissionRepository _rolePermissionRepository;
    private readonly IAuthorizationUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RolePermissionSeederService(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IRolePermissionRepository rolePermissionRepository,
        IAuthorizationUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _roleRepository = roleRepository
            ?? throw new ArgumentNullException(nameof(roleRepository));
        _permissionRepository = permissionRepository
            ?? throw new ArgumentNullException(nameof(permissionRepository));
        _rolePermissionRepository = rolePermissionRepository
            ?? throw new ArgumentNullException(nameof(rolePermissionRepository));
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
        _dateTimeProvider = dateTimeProvider
            ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var nowUtc = _dateTimeProvider.UtcNow;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var roleEntry in DefaultRolePermissions.All)
            {
                var normalizedRoleName = Normalize(roleEntry.Key);

                var role = await _roleRepository.GetByNameNormalizedAsync(
                    normalizedRoleName,
                    cancellationToken);

                if (role is null)
                {
                    throw new InvalidOperationException(
                        $"Built-in role '{roleEntry.Key}' was not found before role-permission seeding.");
                }

                foreach (var permissionKey in roleEntry.Value)
                {
                    var normalizedPermissionKey = Normalize(permissionKey);

                    var permission = await _permissionRepository.GetByKeyNormalizedAsync(
                        normalizedPermissionKey,
                        cancellationToken);

                    if (permission is null)
                    {
                        throw new InvalidOperationException(
                            $"Built-in permission '{permissionKey}' was not found before role-permission seeding.");
                    }

                    var existingGrant = await _rolePermissionRepository.GetByRoleIdAndPermissionIdAsync(
                        role.RoleId,
                        permission.PermissionId,
                        cancellationToken);

                    if (existingGrant is not null)
                    {
                        continue;
                    }

                    var newGrant = RolePermission.CreateNew(
                        roleId: role.RoleId,
                        permissionId: permission.PermissionId,
                        grantedAt: nowUtc,
                        grantedByUserId: null);

                    await _rolePermissionRepository.InsertAsync(
                        newGrant,
                        cancellationToken);
                }
            }

            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToUpperInvariant();
    }
}