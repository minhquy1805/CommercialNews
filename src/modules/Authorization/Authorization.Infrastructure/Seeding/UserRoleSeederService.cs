using Authorization.Application.Ports.Persistence;
using Authorization.Application.Ports.Services;
using Authorization.Domain.Constants;
using Authorization.Domain.Entities;
using Authorization.Infrastructure.Configuration;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Microsoft.Extensions.Options;

namespace Authorization.Infrastructure.Seeding;

public sealed class UserRoleSeederService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IAuthorizationUserLookupService _authorizationUserLookupService;
    private readonly IAuthorizationUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly DefaultAdminSettings _defaultAdminSettings;

    public UserRoleSeederService(
        IRoleRepository roleRepository,
        IUserRoleRepository userRoleRepository,
        IAuthorizationUserLookupService authorizationUserLookupService,
        IAuthorizationUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IOptions<DefaultAdminSettings> defaultAdminOptions)
    {
        _roleRepository = roleRepository
            ?? throw new ArgumentNullException(nameof(roleRepository));
        _userRoleRepository = userRoleRepository
            ?? throw new ArgumentNullException(nameof(userRoleRepository));
        _authorizationUserLookupService = authorizationUserLookupService
            ?? throw new ArgumentNullException(nameof(authorizationUserLookupService));
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
        _dateTimeProvider = dateTimeProvider
            ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _defaultAdminSettings = defaultAdminOptions?.Value
            ?? throw new ArgumentNullException(nameof(defaultAdminOptions));
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!_defaultAdminSettings.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_defaultAdminSettings.Email))
        {
            throw new InvalidOperationException(
                "Authorization default admin bootstrap is enabled but Email is missing.");
        }

        var adminRole = await _roleRepository.GetByNameNormalizedAsync(
            Normalize(SystemRoles.Admin),
            cancellationToken);

        if (adminRole is null)
        {
            throw new InvalidOperationException(
                "Built-in Admin role was not found before user-role seeding.");
        }

        var adminUser = await _authorizationUserLookupService.GetByEmailAsync(
            _defaultAdminSettings.Email.Trim(),
            cancellationToken);

        if (adminUser is null)
        {
            throw new InvalidOperationException(
                $"Default admin user '{_defaultAdminSettings.Email}' was not found in Identity.");
        }

        var existingAssignment = await _userRoleRepository.GetByUserIdAndRoleIdAsync(
            adminUser.UserId,
            adminRole.RoleId,
            cancellationToken);

        if (existingAssignment is not null)
        {
            return;
        }

        var nowUtc = _dateTimeProvider.UtcNow;

        var newAssignment = UserRole.CreateNew(
            userId: adminUser.UserId,
            roleId: adminRole.RoleId,
            assignedAt: nowUtc,
            assignedByUserId: null);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            await _userRoleRepository.InsertAsync(
                newAssignment,
                cancellationToken);

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