namespace Notifications.Domain.Exceptions;

public sealed class NotificationsDomainException : Exception
{
    public string Code { get; }

    public NotificationsDomainException(string code, string message)
        : base(message)
    {
        Code = code;
    }
}