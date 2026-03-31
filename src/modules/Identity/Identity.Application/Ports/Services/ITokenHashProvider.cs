namespace Identity.Application.Ports.Services
{
    public interface ITokenHashProvider
    {
        byte[] Hash(string rawToken);
    }
}