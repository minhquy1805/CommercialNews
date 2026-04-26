namespace Identity.Application.Ports.Services;

public interface IIdentityDataInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}