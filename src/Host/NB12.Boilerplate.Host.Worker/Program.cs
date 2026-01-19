using NB12.Boilerplate.BuildingBlocks.Application.Modularity;
using NB12.Boilerplate.Host.Worker;
using NB12.Boilerplate.Host.Worker.Modules;

var builder = Host.CreateApplicationBuilder(args);

var serviceModules = ModuleRegistration.ServiceModules();

foreach (var module in serviceModules)
{
    module.AddModule(builder.Services, builder.Configuration);
}

//builder.Services.AddMediatR(cfg =>
//{
//    cfg.RegisterServicesFromAssemblies(ModuleCatalog.GetApplicationAssemblies(serviceModules));
//});


builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
