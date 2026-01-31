using NB12.Boilerplate.BuildingBlocks.Api.Modularity;
using NB12.Boilerplate.BuildingBlocks.Application.Eventing;
using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration;
using NB12.Boilerplate.BuildingBlocks.Infrastructure;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.EventBus;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Eventing;
using NB12.Boilerplate.Host.Shared;
using NB12.Boilerplate.Host.Worker;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = Host.CreateApplicationBuilder(args);

// Cross-cutting infrastructure (db, current user, permission policies etc.)
// IMPORTANT: pass assemblies if your method supports it (consistent with API host)
builder.Services.AddInfrastructureBuildingBlocks();

// Module loading
var serviceModules = ModuleComposition.ServicesForWorker();
var scanAssemblies = ModuleComposition.AssembliesForWorkerScanning();
var registryAssemblies = ModuleComposition.RegistryAssembliesForWorker();

// Domain eventing + event bus (use service assemblies)
builder.Services.AddDomainEventing(scanAssemblies);
builder.Services.AddEventBus(scanAssemblies);

// Integration Event type registry (services + contracts)
builder.Services.AddSingleton(sp => new IntegrationEventTypeRegistry(registryAssemblies));

//Module DI
foreach (var module in serviceModules)
{
    module.AddModule(builder.Services, builder.Configuration);

    if (module is IWorkerModule workerModule)
        workerModule.AddWorker(builder.Services, builder.Configuration);
}

// Inbox/Outbox options
builder.Services.Configure<InboxOptions>(builder.Configuration.GetSection("Inbox"));
builder.Services.Configure<OutboxOptions>(builder.Configuration.GetSection("Outbox"));

// Worker
builder.Services.AddHostedService<OutboxPublisherWorker>();

// OpenTelemetry (Tracing + Metrics)
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(
        serviceName: "NB12.Boilerplate.Host.Worker",
        serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown"))
    .WithTracing(tracing =>
    {
        tracing
            .AddHttpClientInstrumentation(o => o.RecordException = true)
            .AddEntityFrameworkCoreInstrumentation(o =>
            {
                o.EnrichWithIDbCommand = (activity, command) =>
                {
                    activity.SetTag("db.statement", command.CommandText);
                    activity.SetTag("db.query.text", null);
                    activity.SetTag("db.command_type", command.CommandType.ToString());
                };
            })
            .AddOtlpExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter(InboxMetrics.MeterName)
            .AddOtlpExporter();
    });

var host = builder.Build();
host.Run();
