using CommercialNews.Worker.Messaging.Email.Ports;

namespace CommercialNews.Worker.Messaging.Email.Sending
{
    public sealed class WorkerEmailSender : IWorkerEmailSender
    {
        private readonly ILogger<WorkerEmailSender> _logger;

        public WorkerEmailSender(ILogger<WorkerEmailSender> logger)
        {
            _logger = logger;
        }

        public Task<string?> SendAsync(
            string toEmail,
            string subject,
            string body,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Sending email. To={ToEmail}, Subject={Subject}, Body={Body}",
                toEmail,
                subject,
                body);

            // Phase B:
            // giả lập gửi thành công, trả provider message id giả.
            return Task.FromResult<string?>($"provider-{Guid.NewGuid():N}");
        }
    }
}