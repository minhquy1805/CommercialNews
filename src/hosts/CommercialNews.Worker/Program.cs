
using CommercialNews.BuildingBlocks.DependencyInjection;
using CommercialNews.Worker.CompositionRoot;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddBuildingBlocks(builder.Configuration);
builder.Services.AddWorkerModules(builder.Configuration);

var host = builder.Build();
host.Run();