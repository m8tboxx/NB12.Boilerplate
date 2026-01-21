using NB12.Boilerplate.BuildingBlocks.Api.Modularity;
using NB12.Boilerplate.Modules.Audit.Api;
using NB12.Boilerplate.Modules.Audit.Infrastructure;
using NB12.Boilerplate.Modules.Auth.Api;
using NB12.Boilerplate.Modules.Auth.Infrastructure;
using System.Reflection;

namespace NB12.Boilerplate.Host.Shared
{
    /// <summary>
    /// Single source of truth for module composition.
    /// Host.API and Host.Worker must use this to avoid divergent module registration.
    /// </summary>
    public static class ModuleComposition
    {
        // Central module list. Add/remove modules ONLY here.
        private static readonly IServiceModule[] _serviceModules =
        [
            new AuthApiServicesModule(),
            new AuthServicesModule(),
            new AuditServicesModule(),
            // further modules
        ];

        private static readonly IEndpointModule[] _endpointModules = [
            new AuthEndpointsModule(),
            new AuthAdminEndpointsModule(),
            new AuditEndpointsModule(),
            // further modules
        ];


        /// <summary>
        /// Modules that register DI services.
        /// </summary>
        public static IServiceModule[] ServiceModules() => _serviceModules;

        /// <summary>
        /// Modules that map Minimal API endpoints (Host.API only).
        /// </summary>
        public static IEndpointModule[] EndpointModules() => _endpointModules;

        /// <summary>
        /// Assemblies of modules used for scanning (event registry, MediatR, validators, etc.).
        /// Includes both service and endpoint modules (distinct).
        /// </summary>
        public static Assembly[] ModuleAssemblies()
            => _serviceModules
                .Cast<object>()
                .Concat( _endpointModules.Cast<object>())
                .Select(m => m.GetType().Assembly)
                .Distinct()
                .ToArray();


        /// <summary>
        /// Assemblies that belong to service modules only. Prefer this in Host.Worker
        /// to avoid pulling in API endpoint assemblies unnecessarily.
        /// </summary>
        public static Assembly[] ServiceAssemblies()
            => _serviceModules
                .Select(m => m.GetType().Assembly)
                .Distinct()
                .ToArray();
    }
}
