namespace CommercialNews.BuildingBlocks.Domain.Exceptions;

public abstract class DomainException : Exception
{
    public string Code { get; }

    protected DomainException(string code, string message)
        : base(message)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Domain exception code is required.", nameof(code));
        }

        Code = code;
    }

    protected DomainException(string code, string message, Exception innerException)
        : base(message, innerException)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Domain exception code is required.", nameof(code));
        }

        Code = code;
    }
}