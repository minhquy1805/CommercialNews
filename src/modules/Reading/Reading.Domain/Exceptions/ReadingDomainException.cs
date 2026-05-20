namespace Reading.Domain.Exceptions;

public sealed class ReadingDomainException : Exception
{
    public ReadingDomainException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}