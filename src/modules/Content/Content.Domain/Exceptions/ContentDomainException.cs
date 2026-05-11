namespace Content.Domain.Exceptions;

public sealed class ContentDomainException : Exception
{
    public string Code { get; }

    public ContentDomainException(string code, string message)
        : base(message)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Exception code is required.", nameof(code));
        }

        Code = code;
    }

    public ContentDomainException(string code, string message, Exception innerException)
        : base(message, innerException)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Exception code is required.", nameof(code));
        }

        Code = code;
    }
}