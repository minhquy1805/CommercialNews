using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Identity.Application.Configuration;
using Identity.Application.Contracts.ForgotPassword;
using Identity.Application.Errors;
using Identity.Application.Ports.Persistence;
using Identity.Application.Ports.Services;
using Identity.Application.Validation.ForgotPassword;
using Identity.Domain.Entities;
using Identity.Domain.Exceptions;
using Microsoft.Extensions.Options;

namespace Identity.Application.UseCases.ForgotPassword;

public sealed class ForgotPasswordUseCase : IForgotPasswordUseCase
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly IRawTokenGenerator _rawTokenGenerator;
    private readonly ITokenHashProvider _tokenHashProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRequestContext _requestContext;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IIdentityOutboxWriter _outboxWriter;
    private readonly IdentityTokenOptions _tokenOptions;

    public ForgotPasswordUseCase(
        IUserAccountRepository userAccountRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        IRawTokenGenerator rawTokenGenerator,
        ITokenHashProvider tokenHashProvider,
        IDateTimeProvider dateTimeProvider,
        IRequestContext requestContext,
        IIdentityUnitOfWork unitOfWork,
        IIdentityOutboxWriter outboxWriter,
        IOptions<IdentityTokenOptions> tokenOptions)
    {
        _userAccountRepository = userAccountRepository ?? throw new ArgumentNullException(nameof(userAccountRepository));
        _passwordResetTokenRepository = passwordResetTokenRepository ?? throw new ArgumentNullException(nameof(passwordResetTokenRepository));
        _rawTokenGenerator = rawTokenGenerator ?? throw new ArgumentNullException(nameof(rawTokenGenerator));
        _tokenHashProvider = tokenHashProvider ?? throw new ArgumentNullException(nameof(tokenHashProvider));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _outboxWriter = outboxWriter ?? throw new ArgumentNullException(nameof(outboxWriter));
        _tokenOptions = tokenOptions?.Value ?? throw new ArgumentNullException(nameof(tokenOptions));
    }

    public async Task<Result<ForgotPasswordResponseDto>> ExecuteAsync(
        ForgotPasswordRequestDto request,
        CancellationToken cancellationToken = default)
    {
        Error? validationError = ForgotPasswordValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<ForgotPasswordResponseDto>.Failure(validationError);
        }

        try
        {
            string normalizedEmail = NormalizeEmail(request.Email);

            UserAccount? user = await _userAccountRepository.GetByEmailNormalizedAsync(
                normalizedEmail,
                cancellationToken);

            if (user is null)
            {
                return Result<ForgotPasswordResponseDto>.Success(BuildGenericResponse());
            }

            DateTime nowUtc = _dateTimeProvider.UtcNow;
            string rawResetToken = _rawTokenGenerator.Generate();
            byte[] resetTokenHash = _tokenHashProvider.Hash(rawResetToken);

            DateTime resetTokenExpiresAtUtc =
                nowUtc.AddHours(_tokenOptions.PasswordResetTokenLifetimeHours);

            PasswordResetToken resetToken = PasswordResetToken.Create(
                userId: user.UserId,
                tokenHash: resetTokenHash,
                createdAt: nowUtc,
                expiresAt: resetTokenExpiresAtUtc,
                createdIp: _requestContext.IpAddress,
                correlationId: _requestContext.CorrelationId);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                await _passwordResetTokenRepository.RevokeActiveByUserIdAsync(
                    user.UserId,
                    nowUtc,
                    cancellationToken);

                long resetTokenId = await _passwordResetTokenRepository.InsertAsync(
                    resetToken,
                    cancellationToken);

                await _outboxWriter.EnqueuePasswordResetRequestedAsync(
                    unitOfWork: _unitOfWork,
                    userId: user.UserId,
                    userPublicId: user.PublicId,
                    email: user.Email,
                    fullName: user.FullName,
                    resetTokenId: resetTokenId,
                    rawResetToken: rawResetToken,
                    expiresAtUtc: resetToken.ExpiresAt,
                    occurredAtUtc: nowUtc,
                    cancellationToken: cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<ForgotPasswordResponseDto>.Success(BuildGenericResponse());
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (PersistenceException)
        {
            return Result<ForgotPasswordResponseDto>.Failure(
                IdentityErrors.PasswordReset.RequestFailed);
        }
        catch (IdentityDomainException)
        {
            return Result<ForgotPasswordResponseDto>.Failure(
                IdentityErrors.PasswordReset.RequestFailed);
        }
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToUpperInvariant();
    }

    private static ForgotPasswordResponseDto BuildGenericResponse()
    {
        return new ForgotPasswordResponseDto
        {
            Requested = true,
            Message = "If the account exists, a password reset email will be sent."
        };
    }
}