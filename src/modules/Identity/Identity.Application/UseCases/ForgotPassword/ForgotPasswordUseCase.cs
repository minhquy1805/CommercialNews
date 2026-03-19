using CommercialNews.BuildingBlocks.Messaging.Outbox;
using Identity.Application.Contracts;
using Identity.Application.Contracts.Dtos;
using Identity.Application.Contracts.Ports;
using Identity.Domain.Entities;

namespace Identity.Application.UseCases.ForgotPassword
{
    public sealed class ForgotPasswordUseCase : IForgotPasswordUseCase
    {
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
        private readonly IRawTokenGenerator _rawTokenGenerator;
        private readonly ITokenHashProvider _tokenHashProvider;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IOutboxWriter _outboxWriter;
        private readonly IIdentityUnitOfWork _unitOfWork;
        private readonly IPublicIdGenerator _publicIdGenerator;

        public ForgotPasswordUseCase(
            IUserAccountRepository userAccountRepository,
            IPasswordResetTokenRepository passwordResetTokenRepository,
            IRawTokenGenerator rawTokenGenerator,
            ITokenHashProvider tokenHashProvider,
            IDateTimeProvider dateTimeProvider,
            IOutboxWriter outboxWriter,
            IIdentityUnitOfWork unitOfWork,
            IPublicIdGenerator publicIdGenerator)
        {
            _userAccountRepository = userAccountRepository;
            _passwordResetTokenRepository = passwordResetTokenRepository;
            _rawTokenGenerator = rawTokenGenerator;
            _tokenHashProvider = tokenHashProvider;
            _dateTimeProvider = dateTimeProvider;
            _outboxWriter = outboxWriter;
            _unitOfWork = unitOfWork;
            _publicIdGenerator = publicIdGenerator;
        }

        public async Task<ForgotPasswordResponseDto> ExecuteAsync(
            ForgotPasswordRequestDto request,
            CancellationToken cancellationToken)
        {
            ValidateRequest(request);

            var normalizedEmail = NormalizeEmail(request.Email);
            var user = await _userAccountRepository.GetByEmailNormalizedAsync(
                normalizedEmail,
                cancellationToken);

            if (user is null)
            {
                return BuildGenericResponse();
            }

            var nowUtc = _dateTimeProvider.UtcNow;
            var rawResetToken = _rawTokenGenerator.Generate();
            var resetTokenHash = _tokenHashProvider.Hash(rawResetToken);

            var resetToken = new PasswordResetToken(
                resetTokenId: 0,
                userId: user.UserId,
                tokenHash: resetTokenHash,
                expiresAt: nowUtc.AddHours(1),
                usedAt: null,
                revokedAt: null,
                createdAt: nowUtc,
                createdIp: null,
                correlationId: null);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                await _passwordResetTokenRepository.RevokeActiveByUserIdAsync(
                    user.UserId,
                    cancellationToken);

                await _passwordResetTokenRepository.InsertAsync(
                    resetToken,
                    cancellationToken);

                var payload = new PasswordResetRequestedPayloadDto
                {
                    UserId = user.UserId,
                    PublicId = user.PublicId,
                    Email = user.Email,
                    RawToken = rawResetToken
                };

                var payloadJson = System.Text.Json.JsonSerializer.Serialize(payload);

                var outboxMessageId = _publicIdGenerator.NewId();

                await _outboxWriter.WriteAsync(
                    messageId: outboxMessageId,
                    eventType: IdentityOutboxEventTypes.PasswordResetRequested,
                    aggregateType: "UserAccount",
                    aggregateId: user.UserId.ToString(),
                    aggregatePublicId: user.PublicId,
                    aggregateVersion: user.Version,
                    payload: payloadJson,
                    headers: null,
                    correlationId: null,
                    initiatorUserId: user.UserId,
                    occurredAtUtc: nowUtc,
                    cancellationToken: cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }

            return BuildGenericResponse();
        }

        private static void ValidateRequest(ForgotPasswordRequestDto request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                throw new ArgumentException("Email is required.", nameof(request.Email));
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
}