namespace Seo.Domain.Exceptions;

public sealed class SeoDomainException : Exception
{
    public string Code { get; }

    public SeoDomainException(string code, string message)
        : base(message)
    {
        Code = code;
    }
}