namespace Media.Domain.Exceptions;

public sealed class MediaDomainException : Exception
{
    public string Code { get; }

    public MediaDomainException(string code, string message)
        : base(message)
    {
        Code = code;
    }
}