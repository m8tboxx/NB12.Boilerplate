using NB12.Boilerplate.BuildingBlocks.Web.Modularity;
using NB12.Boilerplate.Modules.Audit.Infrastructure;
using NB12.Boilerplate.Modules.Auth.Infrastructure;

namespace NB12.Boilerplate.Host.Worker.Modules
{
    public static class ModuleRegistration
    {
        public static IModuleServices[] ServiceModules() => 
            [
                new AuditServicesModule(),
                new AuthServicesModule(),
            ];
    }
}
