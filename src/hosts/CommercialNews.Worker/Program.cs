using CommercialNews.Worker.HostedServices;
using CommercialNews.Worker.Messaging.Outbox.Ports;
using CommercialNews.Worker.Messaging.Outbox.Sql;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<WorkerSqlConnectionFactory>();

builder.Services.AddScoped<IOutboxMessageReader, SqlOutboxMessageReader>();
builder.Services.AddScoped<IOutboxMessageStateRepository, SqlOutboxMessageStateRepository>();

builder.Services.AddHostedService<OutboxPollingService>();

var host = builder.Build();
host.Run();