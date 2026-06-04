using CommercialNews.BuildingBlocks.Initialization;
using CommercialNews.BuildingBlocks.SharedKernel.Identifiers;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Identity.Application.Ports.Persistence;
using Identity.Application.Ports.Services;
using Identity.Domain.Entities;
using Identity.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Identity.Infrastructure.Seeding;

public sealed class DefaultAdminInitializer : IDataInitializer
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPublicIdGenerator _publicIdGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly DefaultAdminSettings _defaultAdminSettings;

    public DefaultAdminInitializer(
        IUserAccountRepository userAccountRepository,
        IIdentityUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IPublicIdGenerator publicIdGenerator,
        IDateTimeProvider dateTimeProvider,
        IOptions<DefaultAdminSettings> defaultAdminOptions)
    {
        _userAccountRepository = userAccountRepository
            ?? throw new ArgumentNullException(nameof(userAccountRepository));
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
        _passwordHasher = passwordHasher
            ?? throw new ArgumentNullException(nameof(passwordHasher));
        _publicIdGenerator = publicIdGenerator
            ?? throw new ArgumentNullException(nameof(publicIdGenerator));
        _dateTimeProvider = dateTimeProvider
            ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _defaultAdminSettings = defaultAdminOptions?.Value
            ?? throw new ArgumentNullException(nameof(defaultAdminOptions));
    }

    public int Order => InitializationOrders.Identity;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!_defaultAdminSettings.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_defaultAdminSettings.Email))
        {
            throw new InvalidOperationException(
                "Identity default admin bootstrap is enabled but Email is missing.");
        }

        if (string.IsNullOrWhiteSpace(_defaultAdminSettings.Password))
        {
            throw new InvalidOperationException(
                "Identity default admin bootstrap is enabled but Password is missing.");
        }

        var normalizedEmail = NormalizeEmail(_defaultAdminSettings.Email);

        var existingUser = await _userAccountRepository.GetByEmailNormalizedAsync(
            normalizedEmail,
            cancellationToken);

        if (existingUser is not null)
        {
            return;
        }

        var nowUtc = _dateTimeProvider.UtcNow;
        var passwordHash = _passwordHasher.Hash(_defaultAdminSettings.Password);

        var newUser = UserAccount.CreateBootstrapAdmin(
            publicId: _publicIdGenerator.NewId(),
            email: _defaultAdminSettings.Email.Trim(),
            emailNormalized: normalizedEmail,
            passwordHash: passwordHash,
            fullName: NormalizeOptional(_defaultAdminSettings.FullName),
            avatarUrl: null,
            nowUtc: nowUtc);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            await _userAccountRepository.InsertBootstrapAdminAsync(
                newUser,
                cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToUpperInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
