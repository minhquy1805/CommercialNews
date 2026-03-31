using CommercialNews.BuildingBlocks.Abstractions.Time;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.Results;
using Identity.Application.Contracts.Requests;
using Identity.Application.Contracts.Responses;
using Identity.Application.Errors;
using Identity.Application.Ports.Persistence;
using Identity.Application.Ports.Services;
using Identity.Domain.Entities;
using Identity.Domain.Exceptions;

namespace Identity.Application.UseCases.VerifyEmail
{
    public sealed class VerifyEmailUseCase : IVerifyEmailUseCase
    {
        private readonly ITokenHashProvider _tokenHashProvider;
        private readonly IEmailVerificationTokenRepository _emailVerificationTokenRepository;
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IIdentityUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _dateTimeProvider;

        public VerifyEmailUseCase(
            ITokenHashProvider tokenHashProvider,
            IEmailVerificationTokenRepository emailVerificationTokenRepository,
            IUserAccountRepository userAccountRepository,
            IIdentityUnitOfWork unitOfWork,
            IDateTimeProvider dateTimeProvider)
        {
            _tokenHashProvider = tokenHashProvider;
            _emailVerificationTokenRepository = emailVerificationTokenRepository;
            _userAccountRepository = userAccountRepository;
            _unitOfWork = unitOfWork;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Result<VerifyEmailResponseDto>> ExecuteAsync(
            VerifyEmailRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request is null)
            {
                return Result<VerifyEmailResponseDto>.Failure(IdentityErrors.ValidationFailed);
            }

            if (string.IsNullOrWhiteSpace(request.Token))
            {
                return Result<VerifyEmailResponseDto>.Failure(IdentityErrors.ValidationFailed);
            }

            try
            {
                byte[] tokenHash = _tokenHashProvider.Hash(request.Token);

                EmailVerificationToken? token =
                    await _emailVerificationTokenRepository.GetActiveByTokenHashAsync(
                        tokenHash,
                        cancellationToken);

                if (token is null)
                {
                    return Result<VerifyEmailResponseDto>.Failure(
                        IdentityErrors.EmailVerification.TokenNotFound);
                }

                DateTime nowUtc = _dateTimeProvider.UtcNow;

                token.MarkUsed(nowUtc);

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                try
                {
                    bool markedUsed = await _emailVerificationTokenRepository.MarkUsedAsync(
                        token.VerificationTokenId,
                        cancellationToken);

                    if (!markedUsed)
                    {
                        await _unitOfWork.RollbackAsync(cancellationToken);
                        return Result<VerifyEmailResponseDto>.Failure(
                            IdentityErrors.EmailVerification.TokenAlreadyUsed);
                    }

                    bool verified = await _userAccountRepository.SetEmailVerifiedAsync(
                        token.UserId,
                        cancellationToken);

                    if (!verified)
                    {
                        await _unitOfWork.RollbackAsync(cancellationToken);
                        return Result<VerifyEmailResponseDto>.Failure(
                            IdentityErrors.User.AlreadyVerified);
                    }

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
                "IDENTITY.EMAIL_VERIFICATION_INVALID_USED_AT" => IdentityErrors.EmailVerification.InvalidUsedAt,
                "IDENTITY.EMAIL_VERIFICATION_TOKEN_HASH_REQUIRED" => IdentityErrors.EmailVerification.TokenHashRequired,
                "IDENTITY.EMAIL_VERIFICATION_TOKEN_HASH_INVALID" => IdentityErrors.EmailVerification.TokenHashInvalid,
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
}