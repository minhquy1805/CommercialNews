namespace Audit.Domain.Exceptions;

public sealed class AuditDomainException : Exception
{
    public string Code { get; }

    public AuditDomainException(string code, string message)
        : base(message)
    {
        Code = code;
    }
}