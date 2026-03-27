namespace Content.Domain.Exceptions;

public sealed class ContentDomainException : Exception
{
    public string Code { get; }

    public ContentDomainException(string code, string message)
        : base(message)
    {
        Code = code;
    }
}