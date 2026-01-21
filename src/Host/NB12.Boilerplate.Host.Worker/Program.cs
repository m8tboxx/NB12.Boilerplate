using NB12.Boilerplate.BuildingBlocks.Application.Eventing;
using NB12.Boilerplate.BuildingBlocks.Infrastructure;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.EventBus;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Eventing;
using NB12.Boilerplate.Host.Worker;
using NB12.Boilerplate.Host.Worker.Modules;
using NB12.Boilerplate.Modules.Audit.Contracts.IntegrationEvents;
using NB12.Boilerplate.Modules.Auth.Contracts.IntegrationEvents;

var builder = Host.CreateApplicationBuilder(args);

var serviceModules = ModuleRegistration.ServiceModules();
var moduleAssemblies = serviceModules.Select(m => m.ApplicationAssembly).Distinct().ToArray();

builder.Services.AddInfrastructureBuildingBlocks();

builder.Services.Configure<OutboxOptions>(builder.Configuration.GetSection("Outbox"));

foreach (var module in serviceModules)
    module.AddModule(builder.Services, builder.Configuration);

builder.Services.AddDomainEventing(moduleAssemblies);
builder.Services.AddEventBus(moduleAssemblies);

var contractsAssemblies = new[]
{
    typeof(UserCreatedIntegrationEvent).Assembly,
    typeof(AuditableEntitiesChangedIntegrationEvent).Assembly
};

var registryAssemblies = moduleAssemblies
    .Concat(contractsAssemblies)
    .Distinct()
    .ToArray();

builder.Services.Configure<OutboxOptions>(builder.Configuration.GetSection("Outbox"));
builder.Services.AddSingleton(sp => new IntegrationEventTypeRegistry(registryAssemblies));
builder.Services.AddHostedService<OutboxPublisherWorker>();

var host = builder.Build();
host.Run();
