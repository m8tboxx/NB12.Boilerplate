using NB12.Boilerplate.BuildingBlocks.Application.Eventing;
using NB12.Boilerplate.BuildingBlocks.Infrastructure;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.EventBus;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Eventing;
using NB12.Boilerplate.Host.Shared;
using NB12.Boilerplate.Host.Worker;
using NB12.Boilerplate.Modules.Audit.Contracts.IntegrationEvents;
using NB12.Boilerplate.Modules.Auth.Contracts.IntegrationEvents;

var builder = Host.CreateApplicationBuilder(args);

// Module loading
var serviceModules = ModuleComposition.ServiceModules();
var serviceAssemblies = ModuleComposition.ServiceAssemblies();

// Contracts assemblies (integration event contracts live in separate projects)
var contractsAssemblies = new[]
{
    typeof(UserCreatedIntegrationEvent).Assembly,
    typeof(AuditableEntitiesChangedIntegrationEvent).Assembly
};

// Assemblies used for registries/scanning (services + contracts)
var registryAssemblies = serviceAssemblies
    .Concat(contractsAssemblies)
    .Distinct()
    .ToArray();

// Cross-cutting infrastructure (db, current user, permission policies etc.)
// IMPORTANT: pass assemblies if your method supports it (consistent with API host)
builder.Services.AddInfrastructureBuildingBlocks(serviceAssemblies);

// Domain eventing + event bus (use service assemblies)
builder.Services.AddDomainEventing(serviceAssemblies);
builder.Services.AddEventBus(serviceAssemblies);

// Outbox options
builder.Services.Configure<OutboxOptions>(builder.Configuration.GetSection("Outbox"));

//Module DI
foreach (var module in serviceModules)
    module.AddModule(builder.Services, builder.Configuration);

// Integration Event type registry (services + contracts)
builder.Services.AddSingleton(sp => new IntegrationEventTypeRegistry(registryAssemblies));

// Worker
builder.Services.AddHostedService<OutboxPublisherWorker>();

var host = builder.Build();
host.Run();
