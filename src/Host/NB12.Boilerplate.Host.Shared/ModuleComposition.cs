using NB12.Boilerplate.BuildingBlocks.Api.Modularity;
using NB12.Boilerplate.Host.Shared.Ops;
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
        private static readonly IServiceModule[] _serviceModules =
        [
            new AuditServicesModule(),
            new AuthServicesModule(),
            new AuthApiServicesModule(),
            // further modules …
    ];

        private static readonly IEndpointModule[] _endpointModules =
        [
            new AuthEndpointsModule(),
            new AuthAdminEndpointsModule(),
            new AuditEndpointsModule(),
            new AuditAdminEndpointsModule(),
            new OpsEndpointsModule(),
            // further modules …
    ];

        public static IServiceModule[] ServiceModules() => _serviceModules;

        public static IEndpointModule[] EndpointModules() => _endpointModules;

        /// <summary>
        /// Assemblies for scanning in Host.API (service + application + endpoint).
        /// </summary>
        public static Assembly[] ModuleAssemblies()
        {
            foreach (var m in _serviceModules)
            {
                if (m.ApplicationAssembly is null)
                    throw new InvalidOperationException($"{m.GetType().Name} must provide ApplicationAssembly.");
            }
            // Service assemblies + corresponding Application assemblies
            var serviceAndApplication = _serviceModules
                .SelectMany(m => new[] { m.GetType().Assembly, m.ApplicationAssembly });

            // Endpoint assemblies (API)
            var endpoints = _endpointModules
                .Select(m => m.GetType().Assembly);

            return serviceAndApplication
                .Concat(endpoints)
                .Distinct()
                .ToArray();
        }

        /// <summary>
        /// Assemblies for scanning in Host.Worker (service + application only).
        /// </summary>
        public static Assembly[] ServiceAssemblies()
            => _serviceModules
                .SelectMany(m => new[] { m.GetType().Assembly, m.ApplicationAssembly })
                .Distinct()
                .ToArray();
    }
}
