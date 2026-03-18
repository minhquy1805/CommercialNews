namespace Identity.Application.Contracts.Ports
{
    public interface ITokenHashProvider
    {
        byte[] Hash(string rawToken);
    }
}
