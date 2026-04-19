namespace Authorization.Domain.Exceptions;

public sealed class AuthorizationDomainException : Exception
{
    public string Code { get; }

    public AuthorizationDomainException(string code, string message)
        : base(message)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Exception code is required.", nameof(code));
        }

        Code = code;
    }

    public AuthorizationDomainException(string code, string message, Exception innerException)
        : base(message, innerException)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Exception code is required.", nameof(code));
        }

        Code = code;
    }
}