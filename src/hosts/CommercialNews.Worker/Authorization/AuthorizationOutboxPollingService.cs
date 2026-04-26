using CommercialNews.BuildingBlocks.Outbox.Runtime;
using CommercialNews.Worker.Configuration;
using Microsoft.Extensions.Options;

namespace CommercialNews.Worker.Authorization;

public sealed class AuthorizationOutboxPollingService : BaseOutboxPollingService
{
    public AuthorizationOutboxPollingService(
        IServiceScopeFactory serviceScopeFactory,
        IOptionsMonitor<OutboxWorkerOptions> optionsMonitor,
        ILogger<AuthorizationOutboxPollingService> logger)
        : base(serviceScopeFactory, optionsMonitor, logger)
    {
    }

    protected override string WorkerName => "Authorization";

    protected override string OptionsName => OutboxWorkerOptionNames.AuthorizationOutbox;
}