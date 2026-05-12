namespace Content.Domain.Exceptions;

public sealed class ContentDomainException : Exception
{
    public string Code { get; }

    public ContentDomainException(string code, string message)
        : base(message)
    {
        Code = ValidateCode(code);
    }

    public ContentDomainException(string code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = ValidateCode(code);
    }

    private static string ValidateCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Exception code is required.", nameof(code));
        }

        return code;
    }
}
