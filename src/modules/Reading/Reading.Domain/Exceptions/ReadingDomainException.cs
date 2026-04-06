namespace Reading.Domain.Exceptions;

public sealed class ReadingDomainException : Exception
{
    public string Code { get; }

    public ReadingDomainException(string code, string message)
        : base(message)
    {
        Code = code;
    }
}