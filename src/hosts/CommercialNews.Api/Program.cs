using CommercialNews.Api.CompositionRoot;
using CommercialNews.Api.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Host-level registrations
builder.Services.AddHostServices(builder.Configuration);

// Module registrations
builder.Services.AddApplicationModules(builder.Configuration);

var app = builder.Build();

app.UseHostOpenApi();

app.UseHttpsRedirection();

app.UseHostPipeline();

app.Run();