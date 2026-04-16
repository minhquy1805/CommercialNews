namespace Identity.Domain.Exceptions;

public sealed class IdentityDomainException : Exception
{
    public string Code { get; }

    public IdentityDomainException(string code, string message)
        : base(message)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Exception code must not be null or empty.", nameof(code));
        }

        Code = code;
    }

    public IdentityDomainException(string code, string message, Exception innerException)
        : base(message, innerException)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Exception code must not be null or empty.", nameof(code));
        }

        Code = code;
    }
}