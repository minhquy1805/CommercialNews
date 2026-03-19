namespace CommercialNews.Worker.Messaging.Email.Ports
{
    public interface IWorkerEmailSender
    {
        Task<string?> SendAsync(
            string toEmail,
            string subject,
            string body,
            CancellationToken cancellationToken);
    }
}