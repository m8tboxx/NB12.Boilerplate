using NB12.Boilerplate.BuildingBlocks.Application.Eventing;
using NB12.Boilerplate.BuildingBlocks.Infrastructure;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.EventBus;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Eventing;
using NB12.Boilerplate.Host.Worker;
using NB12.Boilerplate.Host.Worker.Modules;

var builder = Host.CreateApplicationBuilder(args);

var serviceModules = ModuleRegistration.ServiceModules();
var moduleAssemblies = serviceModules.Select(m => m.ApplicationAssembly).Distinct().ToArray();

builder.Services.AddInfrastructureBuildingBlocks();

builder.Services.Configure<OutboxOptions>(builder.Configuration.GetSection("Outbox"));

foreach (var module in serviceModules)
    module.AddModule(builder.Services, builder.Configuration);

builder.Services.AddDomainEventing(moduleAssemblies);
builder.Services.AddEventBus(moduleAssemblies);

builder.Services.Configure<OutboxOptions>(builder.Configuration.GetSection("Outbox"));
builder.Services.AddSingleton(sp => new IntegrationEventTypeRegistry(moduleAssemblies));
builder.Services.AddHostedService<OutboxPublisherWorker>();

//builder.Services.AddSingleton(sp =>
//{
//    // Re-use the same module assemblies you already have available via ModuleRegistration
//    var serviceModules = ModuleRegistration.ServiceModules();
//    var assemblies = serviceModules.Select(m => m.ApplicationAssembly).Distinct().ToArray();
//    return new IntegrationEventTypeRegistry(assemblies);
//});

//// Eventbus + Json
//builder.Services.AddSingleton(new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
//builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();

//builder.Services.AddHostedService<OutboxPublisherWorker>();

//builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
