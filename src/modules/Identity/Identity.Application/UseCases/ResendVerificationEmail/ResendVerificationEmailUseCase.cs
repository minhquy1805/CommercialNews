using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Identity.Application.Configuration;
using Identity.Application.Contracts.ResendVerificationEmail;
using Identity.Application.Errors;
using Identity.Application.Ports.Persistence;
using Identity.Application.Ports.Services;
using Identity.Application.Validation.ResendVerificationEmail;
using Identity.Domain.Entities;
using Identity.Domain.Exceptions;
using Microsoft.Extensions.Options;

namespace Identity.Application.UseCases.ResendVerificationEmail;

public sealed class ResendVerificationEmailUseCase : IResendVerificationEmailUseCase
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IEmailVerificationTokenRepository _emailVerificationTokenRepository;
    private readonly IRawTokenGenerator _rawTokenGenerator;
    private readonly ITokenHashProvider _tokenHashProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IIdentityOutboxWriter _outboxWriter;
    private readonly IdentityTokenOptions _tokenOptions;

    public ResendVerificationEmailUseCase(
        IUserAccountRepository userAccountRepository,
        IEmailVerificationTokenRepository emailVerificationTokenRepository,
        IRawTokenGenerator rawTokenGenerator,
        ITokenHashProvider tokenHashProvider,
        IDateTimeProvider dateTimeProvider,
        IIdentityUnitOfWork unitOfWork,
        IIdentityOutboxWriter outboxWriter,
        IOptions<IdentityTokenOptions> tokenOptions)
    {
        _userAccountRepository = userAccountRepository ?? throw new ArgumentNullException(nameof(userAccountRepository));
        _emailVerificationTokenRepository = emailVerificationTokenRepository ?? throw new ArgumentNullException(nameof(emailVerificationTokenRepository));
        _rawTokenGenerator = rawTokenGenerator ?? throw new ArgumentNullException(nameof(rawTokenGenerator));
        _tokenHashProvider = tokenHashProvider ?? throw new ArgumentNullException(nameof(tokenHashProvider));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _outboxWriter = outboxWriter ?? throw new ArgumentNullException(nameof(outboxWriter));
        _tokenOptions = tokenOptions?.Value ?? throw new ArgumentNullException(nameof(tokenOptions));
    }

    public async Task<Result<ResendVerificationEmailResponseDto>> ExecuteAsync(
        ResendVerificationEmailRequestDto request,
        CancellationToken cancellationToken = default)
    {
        Error? validationError = ResendVerificationEmailValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<ResendVerificationEmailResponseDto>.Failure(validationError);
        }

        try
        {
            string normalizedEmail = NormalizeEmail(request.Email);

            var user = await _userAccountRepository.GetByEmailNormalizedAsync(
                normalizedEmail,
                cancellationToken);

            if (user is null)
            {
                return Result<ResendVerificationEmailResponseDto>.Success(BuildGenericResponse());
            }

            if (user.IsEmailVerified)
            {
                return Result<ResendVerificationEmailResponseDto>.Success(BuildGenericResponse());
            }

            DateTime nowUtc = _dateTimeProvider.UtcNow;
            string rawVerificationToken = _rawTokenGenerator.Generate();
            byte[] verificationTokenHash = _tokenHashProvider.Hash(rawVerificationToken);

            DateTime verificationTokenExpiresAtUtc =
                nowUtc.AddHours(_tokenOptions.EmailVerificationTokenLifetimeHours);

            EmailVerificationToken verificationToken = EmailVerificationToken.Create(
                userId: user.UserId,
                tokenHash: verificationTokenHash,
                createdAt: nowUtc,
                expiresAt: verificationTokenExpiresAtUtc,
                createdIp: null,
                correlationId: null);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                long verificationTokenId =
                    await _emailVerificationTokenRepository.InsertAsync(
                        verificationToken,
                        cancellationToken);

                await _outboxWriter.EnqueueVerificationEmailRequestedAsync(
                    unitOfWork: _unitOfWork,
                    userId: user.UserId,
                    userPublicId: user.PublicId,
                    email: user.Email,
                    fullName: user.FullName,
                    verificationTokenId: verificationTokenId,
                    rawVerificationToken: rawVerificationToken,
                    expiresAtUtc: verificationTokenExpiresAtUtc,
                    occurredAtUtc: nowUtc,
                    cancellationToken: cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<ResendVerificationEmailResponseDto>.Success(BuildGenericResponse());
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (PersistenceException)
        {
            return Result<ResendVerificationEmailResponseDto>.Failure(
                IdentityErrors.ResendVerification.RequestFailed);
        }
        catch (IdentityDomainException)
        {
            return Result<ResendVerificationEmailResponseDto>.Failure(
                IdentityErrors.ResendVerification.RequestFailed);
        }
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToUpperInvariant();
    }

    private static ResendVerificationEmailResponseDto BuildGenericResponse()
    {
        return new ResendVerificationEmailResponseDto
        {
            Requested = true,
            Message = "If the account exists and is eligible, a verification email will be sent."
        };
    }
}