using Authorization.Infrastructure.DependencyInjection;
using CommercialNews.Api.CompositionRoot;
using CommercialNews.Api.OpenApi;
using Identity.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Host-level registrations
builder.Services.AddHostServices(builder.Configuration);

// Temporary module registrations
// TODO:
// Identity and Authorization are currently registered directly here
// because these modules were implemented earlier and still need refactoring.
//
// After Content and the next modules are implemented in a cleaner,
// consistent style, move these registrations into ModuleRegistration
// and split them properly into Application/Infrastructure registration methods.
builder.Services.AddIdentityInfrastructure();
builder.Services.AddAuthorizationInfrastructure();

// Future module registration entry point
builder.Services.AddApplicationModules(builder.Configuration);

var app = builder.Build();

app.UseHostOpenApi();

app.UseHttpsRedirection();

app.UseHostPipeline();

app.Run();