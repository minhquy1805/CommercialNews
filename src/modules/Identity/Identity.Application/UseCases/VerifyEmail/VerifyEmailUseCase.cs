using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Identity.Application.Contracts.VerifyEmail;
using Identity.Application.Errors;
using Identity.Application.Ports.Persistence;
using Identity.Application.Ports.Services;
using Identity.Application.Validation.VerifyEmail;
using Identity.Domain.Entities;
using Identity.Domain.Exceptions;

namespace Identity.Application.UseCases.VerifyEmail;

public sealed class VerifyEmailUseCase : IVerifyEmailUseCase
{
    private readonly ITokenHashProvider _tokenHashProvider;
    private readonly IEmailVerificationTokenRepository _emailVerificationTokenRepository;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IIdentityOutboxWriter _outboxWriter;

    public VerifyEmailUseCase(
        ITokenHashProvider tokenHashProvider,
        IEmailVerificationTokenRepository emailVerificationTokenRepository,
        IUserAccountRepository userAccountRepository,
        IIdentityUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IIdentityOutboxWriter outboxWriter)
    {
        _tokenHashProvider = tokenHashProvider ?? throw new ArgumentNullException(nameof(tokenHashProvider));
        _emailVerificationTokenRepository = emailVerificationTokenRepository ?? throw new ArgumentNullException(nameof(emailVerificationTokenRepository));
        _userAccountRepository = userAccountRepository ?? throw new ArgumentNullException(nameof(userAccountRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _outboxWriter = outboxWriter ?? throw new ArgumentNullException(nameof(outboxWriter));
    }

    public async Task<Result<VerifyEmailResponseDto>> ExecuteAsync(
        VerifyEmailRequestDto request,
        CancellationToken cancellationToken = default)
    {
        Error? validationError = VerifyEmailValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<VerifyEmailResponseDto>.Failure(validationError);
        }

        try
        {
            byte[] tokenHash = _tokenHashProvider.Hash(request.Token.Trim());

            EmailVerificationToken? token =
                await _emailVerificationTokenRepository.GetActiveByTokenHashAsync(
                    tokenHash,
                    cancellationToken);

            if (token is null)
            {
                return Result<VerifyEmailResponseDto>.Failure(
                    IdentityErrors.EmailVerification.TokenNotFound);
            }

            UserAccount? user = await _userAccountRepository.GetByIdAsync(
                token.UserId,
                cancellationToken);

            if (user is null)
            {
                return Result<VerifyEmailResponseDto>.Failure(
                    IdentityErrors.User.NotFound);
            }

            DateTime nowUtc = _dateTimeProvider.UtcNow;

            token.MarkUsed(nowUtc);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                bool markedUsed = await _emailVerificationTokenRepository.MarkUsedAsync(
                    token.VerificationTokenId,
                    nowUtc,
                    cancellationToken);

                if (!markedUsed)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);
                    return Result<VerifyEmailResponseDto>.Failure(
                        IdentityErrors.EmailVerification.TokenAlreadyUsed);
                }

                bool verified = await _userAccountRepository.SetEmailVerifiedAsync(
                    token.UserId,
                    nowUtc,
                    cancellationToken);

                if (!verified)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);
                    return Result<VerifyEmailResponseDto>.Failure(
                        IdentityErrors.ValidationFailed);
                }

                await _outboxWriter.EnqueueEmailVerifiedAsync(
                    unitOfWork: _unitOfWork,
                    userId: user.UserId,
                    userPublicId: user.PublicId,
                    email: user.Email,
                    fullName: user.FullName,
                    verificationTokenId: token.VerificationTokenId,
                    verifiedAtUtc: nowUtc,
                    cancellationToken: cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<VerifyEmailResponseDto>.Success(new VerifyEmailResponseDto
                {
                    UserId = token.UserId,
                    Verified = true
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
            return Result<VerifyEmailResponseDto>.Failure(MapPersistenceException(exception));
        }
        catch (IdentityDomainException exception)
        {
            return Result<VerifyEmailResponseDto>.Failure(MapDomainException(exception));
        }
    }

    private static Error MapDomainException(IdentityDomainException exception)
    {
        return exception.Code switch
        {
            "IDENTITY.EMAIL_VERIFICATION_TOKEN_ALREADY_USED" => IdentityErrors.EmailVerification.TokenAlreadyUsed,
            "IDENTITY.EMAIL_VERIFICATION_TOKEN_EXPIRED" => IdentityErrors.EmailVerification.TokenExpired,
            _ => IdentityErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            _ => IdentityErrors.ValidationFailed
        };
    }
}
