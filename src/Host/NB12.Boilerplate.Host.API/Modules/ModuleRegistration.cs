using NB12.Boilerplate.BuildingBlocks.Application.Interfaces;
using NB12.Boilerplate.Modules.Auth.Api;
using NB12.Boilerplate.Modules.Auth.Infrastructure;

namespace NB12.Boilerplate.Host.API.Modules
{
    public static class ModuleRegistration
    {
        public static IModuleServices[] ServiceModules() =>
        [
            new AuthServicesModule(),
            new AuthApiServicesModule(),
            // further Modules …
        ];

        public static IModuleEndpoints[] EndpointModules() =>
        [
            new AuthEndpointsModule(),
            new AuthAdminEndpointsModule(),
            // further Modules …
        ];
    }
}
