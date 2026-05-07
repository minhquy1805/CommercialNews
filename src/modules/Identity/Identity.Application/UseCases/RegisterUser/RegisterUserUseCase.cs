using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Identifiers;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Identity.Application.Configuration;
using Identity.Application.Contracts.RegisterUser;
using Identity.Application.Errors;
using Identity.Application.Ports.Persistence;
using Identity.Application.Ports.Services;
using Identity.Application.Validation.RegisterUser;
using Identity.Domain.Entities;
using Identity.Domain.Exceptions;
using Microsoft.Extensions.Options;

namespace Identity.Application.UseCases.RegisterUser;

public sealed class RegisterUserUseCase : IRegisterUserUseCase
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IEmailVerificationTokenRepository _emailVerificationTokenRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPublicIdGenerator _publicIdGenerator;
    private readonly IRawTokenGenerator _rawTokenGenerator;
    private readonly ITokenHashProvider _tokenHashProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRequestContext _requestContext;
    private readonly IIdentityOutboxWriter _outboxWriter;
    private readonly IdentityTokenOptions _tokenOptions;

    public RegisterUserUseCase(
        IUserAccountRepository userAccountRepository,
        IEmailVerificationTokenRepository emailVerificationTokenRepository,
        IIdentityUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IPublicIdGenerator publicIdGenerator,
        IRawTokenGenerator rawTokenGenerator,
        ITokenHashProvider tokenHashProvider,
        IDateTimeProvider dateTimeProvider,
        IRequestContext requestContext,
        IIdentityOutboxWriter outboxWriter,
        IOptions<IdentityTokenOptions> tokenOptions)
    {
        _userAccountRepository = userAccountRepository ?? throw new ArgumentNullException(nameof(userAccountRepository));
        _emailVerificationTokenRepository = emailVerificationTokenRepository ?? throw new ArgumentNullException(nameof(emailVerificationTokenRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _publicIdGenerator = publicIdGenerator ?? throw new ArgumentNullException(nameof(publicIdGenerator));
        _rawTokenGenerator = rawTokenGenerator ?? throw new ArgumentNullException(nameof(rawTokenGenerator));
        _tokenHashProvider = tokenHashProvider ?? throw new ArgumentNullException(nameof(tokenHashProvider));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
        _outboxWriter = outboxWriter ?? throw new ArgumentNullException(nameof(outboxWriter));
        _tokenOptions = tokenOptions?.Value ?? throw new ArgumentNullException(nameof(tokenOptions));
    }

    public async Task<Result<RegisterUserResponseDto>> ExecuteAsync(
        RegisterUserRequestDto request,
        CancellationToken cancellationToken = default)
    {
        Error? validationError = RegisterUserValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<RegisterUserResponseDto>.Failure(validationError);
        }

        string normalizedEmail = NormalizeEmail(request.Email);
        string email = request.Email.Trim();
        string? fullName = NormalizeOptional(request.FullName);

        try
        {
            UserAccount? existingUser = await _userAccountRepository.GetByEmailNormalizedAsync(
                normalizedEmail,
                cancellationToken);

            if (existingUser is not null)
            {
                return Result<RegisterUserResponseDto>.Failure(IdentityErrors.EmailAlreadyExists);
            }

            DateTime nowUtc = _dateTimeProvider.UtcNow;
            string publicId = _publicIdGenerator.NewId();
            string passwordHash = _passwordHasher.Hash(request.Password);

            UserAccount user = UserAccount.Create(
                publicId: publicId,
                email: email,
                emailNormalized: normalizedEmail,
                passwordHash: passwordHash,
                fullName: fullName,
                avatarUrl: null,
                nowUtc: nowUtc);

            string rawVerificationToken = _rawTokenGenerator.Generate();
            byte[] verificationTokenHash = _tokenHashProvider.Hash(rawVerificationToken);

            DateTime verificationTokenExpiresAtUtc =
                nowUtc.AddHours(_tokenOptions.EmailVerificationTokenLifetimeHours);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                long userId = await _userAccountRepository.InsertAsync(
                    user,
                    cancellationToken);

                EmailVerificationToken verificationToken = EmailVerificationToken.Create(
                    userId: userId,
                    tokenHash: verificationTokenHash,
                    createdAt: nowUtc,
                    expiresAt: verificationTokenExpiresAtUtc,
                    createdIp: _requestContext.IpAddress,
                    correlationId: _requestContext.CorrelationId);

                long verificationTokenId = await _emailVerificationTokenRepository.InsertAsync(
                    verificationToken,
                    cancellationToken);

                await _outboxWriter.EnqueueVerificationEmailRequestedAsync(
                    unitOfWork: _unitOfWork,
                    userId: userId,
                    userPublicId: user.PublicId,
                    email: user.Email,
                    fullName: user.FullName,
                    verificationTokenId: verificationTokenId,
                    rawVerificationToken: rawVerificationToken,
                    expiresAtUtc: verificationTokenExpiresAtUtc,
                    occurredAtUtc: nowUtc,
                    cancellationToken: cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<RegisterUserResponseDto>.Success(new RegisterUserResponseDto
                {
                    UserId = userId,
                    PublicId = user.PublicId,
                    Email = user.Email,
                    RequiresEmailVerification = true
                });
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (PersistenceException exception)
        {
            return Result<RegisterUserResponseDto>.Failure(MapPersistenceException(exception));
        }
        catch (IdentityDomainException)
        {
            return Result<RegisterUserResponseDto>.Failure(IdentityErrors.Register.RequestFailed);
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

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "IDENTITY.EMAIL_EXISTS" => IdentityErrors.EmailAlreadyExists,
            _ => IdentityErrors.Register.RequestFailed
        };
    }
}
