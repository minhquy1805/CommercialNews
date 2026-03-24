namespace Authorization.Application.Contracts.Ports
{
    public interface IPublicIdGenerator
    {
        string NewId();
    }
}