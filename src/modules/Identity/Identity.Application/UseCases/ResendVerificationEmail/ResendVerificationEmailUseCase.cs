using System.Text.Json;
using CommercialNews.BuildingBlocks.Messaging.Outbox;
using Identity.Application.Contracts;
using Identity.Application.Contracts.Payloads;
using Identity.Application.Contracts.Ports;
using Identity.Application.Contracts.Requests;
using Identity.Application.Contracts.Responses;
using Identity.Domain.Entities;

namespace Identity.Application.UseCases.ResendVerificationEmail
{
    public sealed class ResendVerificationEmailUseCase : IResendVerificationEmailUseCase
    {
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IEmailVerificationTokenRepository _emailVerificationTokenRepository;
        private readonly IRawTokenGenerator _rawTokenGenerator;
        private readonly ITokenHashProvider _tokenHashProvider;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IOutboxWriter _outboxWriter;
        private readonly IIdentityUnitOfWork _unitOfWork;
        private readonly IPublicIdGenerator _publicIdGenerator;

        public ResendVerificationEmailUseCase(
            IUserAccountRepository userAccountRepository,
            IEmailVerificationTokenRepository emailVerificationTokenRepository,
            IRawTokenGenerator rawTokenGenerator,
            ITokenHashProvider tokenHashProvider,
            IDateTimeProvider dateTimeProvider,
            IOutboxWriter outboxWriter,
            IIdentityUnitOfWork unitOfWork,
            IPublicIdGenerator publicIdGenerator)
        {
            _userAccountRepository = userAccountRepository;
            _emailVerificationTokenRepository = emailVerificationTokenRepository;
            _rawTokenGenerator = rawTokenGenerator;
            _tokenHashProvider = tokenHashProvider;
            _dateTimeProvider = dateTimeProvider;
            _outboxWriter = outboxWriter;
            _unitOfWork = unitOfWork;
            _publicIdGenerator = publicIdGenerator;
        }

        public async Task<ResendVerificationEmailResponseDto> ExecuteAsync(
            ResendVerificationEmailRequestDto request,
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

            if (user.IsEmailVerified)
            {
                throw new InvalidOperationException("Email is already verified.");
            }

            var nowUtc = _dateTimeProvider.UtcNow;
            var rawVerificationToken = _rawTokenGenerator.Generate();
            var verificationTokenHash = _tokenHashProvider.Hash(rawVerificationToken);

            var verificationToken = new EmailVerificationToken(
                verificationTokenId: 0,
                userId: user.UserId,
                tokenHash: verificationTokenHash,
                expiresAt: nowUtc.AddHours(24),
                usedAt: null,
                createdAt: nowUtc,
                createdIp: null,
                correlationId: null);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                await _emailVerificationTokenRepository.InsertAsync(
                    verificationToken,
                    cancellationToken);

                var payload = new EmailVerificationRequestedPayloadDto
                {
                    UserId = user.UserId,
                    PublicId = user.PublicId,
                    Email = user.Email,
                    RawToken = rawVerificationToken
                };

                var payloadJson = JsonSerializer.Serialize(payload);
                var outboxMessageId = _publicIdGenerator.NewId();

                await _outboxWriter.WriteAsync(
                    messageId: outboxMessageId,
                    eventType: IdentityOutboxEventTypes.EmailVerificationRequested,
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

        private static void ValidateRequest(ResendVerificationEmailRequestDto request)
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

        private static ResendVerificationEmailResponseDto BuildGenericResponse()
        {
            return new ResendVerificationEmailResponseDto
            {
                Requested = true,
                Message = "If the account exists and is not yet verified, a verification email will be sent."
            };
        }
    }
}