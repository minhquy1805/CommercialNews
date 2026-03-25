namespace Authorization.Application.Contracts.Ports
{
    public interface IOutboxMessageIdGenerator
    {
        string NewId();
    }
}