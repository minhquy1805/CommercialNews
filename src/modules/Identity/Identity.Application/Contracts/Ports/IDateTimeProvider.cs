namespace Identity.Application.Contracts.Ports
{
    public interface IDateTimeProvider
    {
        DateTime UtcNow { get; }
    }
}
