namespace Authorization.Application.Ports.Services;

public interface IAuthorizationDataInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}