using CommercialNews.Worker.HostedServices;
using CommercialNews.Worker.Messaging.Email.Configuration;
using CommercialNews.Worker.Messaging.Email.Dispatching;
using CommercialNews.Worker.Messaging.Email.Ports;
using CommercialNews.Worker.Messaging.Email.Sending;
using CommercialNews.Worker.Messaging.Email.Sql;
using CommercialNews.Worker.Messaging.Outbox.Ports;
using CommercialNews.Worker.Messaging.Outbox.Sql;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<MailSettings>(
    builder.Configuration.GetSection("MailSettings"));

builder.Services.AddSingleton<WorkerSqlConnectionFactory>();

builder.Services.AddScoped<IOutboxMessageReader, SqlOutboxMessageReader>();
builder.Services.AddScoped<IOutboxMessageStateRepository, SqlOutboxMessageStateRepository>();

builder.Services.AddScoped<IEmailDeliveryRepository, SqlEmailDeliveryRepository>();
builder.Services.AddScoped<IWorkerEmailSender, MailKitEmailSender>();
// builder.Services.AddScoped<IWorkerEmailSender, WorkerEmailSender>();
builder.Services.AddScoped<IOutboxEventEmailDispatcher, OutboxEventEmailDispatcher>();

builder.Services.AddHostedService<OutboxPollingService>();

var host = builder.Build();
host.Run();