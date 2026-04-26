using Authorization.Application.Ports.Services;

namespace Authorization.Infrastructure.Seeding;

public sealed class AuthorizationDataInitializer : IAuthorizationDataInitializer
{
    private readonly RoleSeederService _roleSeederService;
    private readonly PermissionSeederService _permissionSeederService;
    private readonly RolePermissionSeederService _rolePermissionSeederService;
    private readonly UserRoleSeederService _userRoleSeederService;

    public AuthorizationDataInitializer(
        RoleSeederService roleSeederService,
        PermissionSeederService permissionSeederService,
        RolePermissionSeederService rolePermissionSeederService,
        UserRoleSeederService userRoleSeederService)
    {
        _roleSeederService = roleSeederService
            ?? throw new ArgumentNullException(nameof(roleSeederService));
        _permissionSeederService = permissionSeederService
            ?? throw new ArgumentNullException(nameof(permissionSeederService));
        _rolePermissionSeederService = rolePermissionSeederService
            ?? throw new ArgumentNullException(nameof(rolePermissionSeederService));
        _userRoleSeederService = userRoleSeederService
            ?? throw new ArgumentNullException(nameof(userRoleSeederService));
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _roleSeederService.SeedAsync(cancellationToken);
        await _permissionSeederService.SeedAsync(cancellationToken);
        await _rolePermissionSeederService.SeedAsync(cancellationToken);
        await _userRoleSeederService.SeedAsync(cancellationToken);
    }
}