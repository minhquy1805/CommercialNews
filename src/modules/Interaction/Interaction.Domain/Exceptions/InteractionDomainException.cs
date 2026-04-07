namespace Interaction.Domain.Exceptions;

public sealed class InteractionDomainException : Exception
{
    public string Code { get; }

    public InteractionDomainException(string code, string message)
        : base(message)
    {
        Code = code;
    }
}