using NB12.Boilerplate.Modules.Audit.Infrastructure;
using NB12.Boilerplate.Modules.Auth.Api;
using NB12.Boilerplate.Modules.Auth.Infrastructure;
using NB12.Boilerplate.Modules.Audit.Api;
using NB12.Boilerplate.BuildingBlocks.Web.Modularity;

namespace NB12.Boilerplate.Host.API.Modules
{
    public static class ModuleRegistration
    {
        public static IModuleServices[] ServiceModules() =>
        [
            new AuditServicesModule(),
            new AuthServicesModule(),
            new AuthApiServicesModule(),
            // further Modules …
        ];

        public static IModuleEndpoints[] EndpointModules() =>
        [
            new AuthEndpointsModule(),
            new AuthAdminEndpointsModule(),
            new AuditEndpointsModule(),
            // further Modules …
        ];
    }
}
