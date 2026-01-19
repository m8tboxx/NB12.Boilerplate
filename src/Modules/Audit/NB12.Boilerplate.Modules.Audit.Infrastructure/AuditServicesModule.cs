using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NB12.Boilerplate.BuildingBlocks.Application.Interfaces;
using NB12.Boilerplate.BuildingBlocks.Application.Security;
using NB12.Boilerplate.Modules.Audit.Application;
using NB12.Boilerplate.Modules.Audit.Application.Security;
using System.Reflection;

namespace NB12.Boilerplate.Modules.Audit.Infrastructure
{
    public sealed class AuditServicesModule : IModuleServices
    {
        public string Name => "AuditModule";
        public Assembly ApplicationAssembly => typeof(AssemblyMarker).Assembly;

        public void AddModule(IServiceCollection services, IConfiguration config)
        {
            services.AddAuditInfrastructure(config);

            services.AddSingleton<IPermissionProvider, AuditPermissionProvider>();
        }
    }
}
