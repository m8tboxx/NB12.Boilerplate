using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NB12.Boilerplate.BuildingBlocks.Api.Modularity;
using NB12.Boilerplate.BuildingBlocks.Application.Security;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Inbox;
using NB12.Boilerplate.Modules.Audit.Application;
using NB12.Boilerplate.Modules.Audit.Application.Security;
using NB12.Boilerplate.Modules.Audit.Contracts.IntegrationEvents;
using NB12.Boilerplate.Modules.Audit.Infrastructure.Constants;
using NB12.Boilerplate.Modules.Audit.Infrastructure.Persistence;
using NB12.Boilerplate.Modules.Audit.Infrastructure.Services;
using System.Reflection;

namespace NB12.Boilerplate.Modules.Audit.Infrastructure
{
    public sealed class AuditServicesModule : IServiceModule, IModuleAssemblyProvider, IModuleKeyProvider, IWorkerModule
    {
        public string Name => "AuditModule";
        public string ModuleKey => AuditModule.Key;
        public Assembly ApplicationAssembly => typeof(AssemblyMarker).Assembly;

        
        public void AddModule(IServiceCollection services, IConfiguration config)
        {
            services.AddAuditInfrastructure(config);
            services.AddSingleton<IPermissionProvider, AuditPermissionProvider>();
        }


        public void AddWorker(IServiceCollection services, IConfiguration configuration)
        {
            services.AddInboxWorkerForModule<AuditDbContext>("Audit", configuration);
            services.AddHostedService<AuditRetentionHostedService>();
        }


        public IEnumerable<Assembly> GetAdditionalAssemblies()
            =>
            [
                typeof(AssemblyMarker).Assembly,
                typeof(AuditableEntitiesChangedIntegrationEvent).Assembly,
            ];
    }
}
