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
        private static readonly IServiceModule[] _coreServiceModules =
        [
            new AuditServicesModule(),
            new AuthServicesModule(),
            // further modules …
        ];

        private static readonly IServiceModule[] _apiOnlyServiceModules =
        [
            new AuthApiServicesModule(),
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


        public static IServiceModule[] ServicesForApi()
            => [.. _coreServiceModules, .. _apiOnlyServiceModules];

        public static IServiceModule[] ServicesForWorker()
            => _coreServiceModules;

        public static IEndpointModule[] EndpointModules() => _endpointModules;

        // TODO: DELETE?
        //public static IServiceModule[] ServiceModules() => _coreServiceModules;

        //public static IReadOnlyList<IServiceModule> ApiServiceModules() => _apiOnlyServiceModules;


        public static Assembly[] AssembliesForApiScanning()
        {
            var serviceAssemblies = ServiceAndApplicationAssemblies(ServicesForApi());
            var endpointAssemblies = _endpointModules.Select(m => m.GetType().Assembly);

            return serviceAssemblies.Concat(endpointAssemblies).Distinct().ToArray();
        }


        public static Assembly[] AssembliesForWorkerScanning()
        {
            var serviceAssemblies = ServiceAndApplicationAssemblies(ServicesForWorker());
            return serviceAssemblies.Distinct().ToArray();
        }

        // TODO: DELETE?
        //public static Assembly[] RegistryAssembliesForApi()
        //{
        //    var baseAssemblies = ServiceAndApplicationAssemblies(ServicesForApi());
        //    var extraAssemblies = AdditionalAssemblies(ServicesForApi());

        //    return baseAssemblies.Concat(extraAssemblies).Distinct().ToArray();
        //}


        public static Assembly[] RegistryAssembliesForWorker()
        {
            var baseAssemblies = ServiceAndApplicationAssemblies(ServicesForWorker());
            var extraAssemblies = AdditionalAssemblies(ServicesForWorker());

            return baseAssemblies.Concat(extraAssemblies).Distinct().ToArray();
        }


        private static IEnumerable<Assembly> ServiceAndApplicationAssemblies(IEnumerable<IServiceModule> modules)
        {
            foreach (var module in modules)
            {
                if (module.ApplicationAssembly is null)
                    throw new InvalidOperationException($"{module.GetType().Name} must provide ApplicationAssembly.");
            }

            return modules.SelectMany(m => new[] { m.GetType().Assembly, m.ApplicationAssembly! });
        }


        private static IEnumerable<Assembly> AdditionalAssemblies(IEnumerable<IServiceModule> modules)
            => modules
            .OfType<IModuleAssemblyProvider>()
            .SelectMany(m => m.GetAdditionalAssemblies())
            .Where(a => a is not null)
            .Distinct();
    }
}
