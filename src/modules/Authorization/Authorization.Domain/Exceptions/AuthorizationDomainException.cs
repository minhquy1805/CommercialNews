namespace Authorization.Domain.Exceptions
{
    public sealed class AuthorizationDomainException : Exception
    {
        public string Code { get; }

        public AuthorizationDomainException(string code, string message)
            : base(message)
        {
            Code = code;
        }

        public AuthorizationDomainException(string code, string message, Exception innerException)
            : base(message, innerException)
        {
            Code = code;
        }
    }
}