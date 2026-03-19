using System.Text.Json;
using CommercialNews.Worker.Messaging.Email.Ports;
using CommercialNews.Worker.Messaging.Outbox.Models;
using CommercialNews.Worker.Messaging.Email.Payloads;

namespace CommercialNews.Worker.Messaging.Email.Dispatching
{
    
    public sealed class OutboxEventEmailDispatcher : IOutboxEventEmailDispatcher
    {
        private readonly IEmailDeliveryRepository _emailDeliveryRepository;
        private readonly IWorkerEmailSender _workerEmailSender;

        public OutboxEventEmailDispatcher(
            IEmailDeliveryRepository emailDeliveryRepository,
            IWorkerEmailSender workerEmailSender)
        {
            _emailDeliveryRepository = emailDeliveryRepository;
            _workerEmailSender = workerEmailSender;
        }

        public async Task DispatchAsync(
            OutboxMessageRecord message,
            CancellationToken cancellationToken)
        {
            switch (message.EventType)
            {
                case "identity.email-verification.requested":
                    await HandleEmailVerificationRequestedAsync(message, cancellationToken);
                    break;

                case "identity.password-reset.requested":
                    await HandlePasswordResetRequestedAsync(message, cancellationToken);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported outbox event type: {message.EventType}");
            }
        }

        private async Task HandleEmailVerificationRequestedAsync(
            OutboxMessageRecord message,
            CancellationToken cancellationToken)
        {
            var payload = JsonSerializer.Deserialize<EmailVerificationRequestedPayload>(
                message.Payload,
                JsonSerializerOptions.Default)
                ?? throw new InvalidOperationException("Failed to deserialize email verification payload.");

            var templateKey = "identity-email-verification";
            var subject = "Verify your email";
            var body = $"Hello, please verify your email using this token: {payload.RawToken}";

            var emailDeliveryId = await _emailDeliveryRepository.InsertAsync(
                messageId: message.MessageId,
                userId: payload.UserId,
                toEmail: payload.Email,
                templateKey: templateKey,
                subject: subject,
                correlationId: message.CorrelationId,
                cancellationToken: cancellationToken);

            try
            {
                var providerMessageId = await _workerEmailSender.SendAsync(
                    payload.Email,
                    subject,
                    body,
                    cancellationToken);

                await _emailDeliveryRepository.MarkSentAsync(
                    emailDeliveryId,
                    providerMessageId,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                await _emailDeliveryRepository.MarkFailedAsync(
                    emailDeliveryId,
                    nextRetryAt: DateTime.UtcNow.AddMinutes(1),
                    lastError: ex.Message,
                    cancellationToken: cancellationToken);

                throw;
            }
        }

        private async Task HandlePasswordResetRequestedAsync(
            OutboxMessageRecord message,
            CancellationToken cancellationToken)
        {
            var payload = JsonSerializer.Deserialize<PasswordResetRequestedPayload>(
                message.Payload,
                JsonSerializerOptions.Default)
                ?? throw new InvalidOperationException("Failed to deserialize password reset payload.");

            var templateKey = "identity-password-reset";
            var subject = "Reset your password";
            var body = $"Hello, use this token to reset your password: {payload.RawToken}";

            var emailDeliveryId = await _emailDeliveryRepository.InsertAsync(
                messageId: message.MessageId,
                userId: payload.UserId,
                toEmail: payload.Email,
                templateKey: templateKey,
                subject: subject,
                correlationId: message.CorrelationId,
                cancellationToken: cancellationToken);

            try
            {
                var providerMessageId = await _workerEmailSender.SendAsync(
                    payload.Email,
                    subject,
                    body,
                    cancellationToken);

                await _emailDeliveryRepository.MarkSentAsync(
                    emailDeliveryId,
                    providerMessageId,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                await _emailDeliveryRepository.MarkFailedAsync(
                    emailDeliveryId,
                    nextRetryAt: DateTime.UtcNow.AddMinutes(1),
                    lastError: ex.Message,
                    cancellationToken: cancellationToken);

                throw;
            }
        }

        
    }
}