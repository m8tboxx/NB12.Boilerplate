using System.Reflection;

namespace NB12.Boilerplate.BuildingBlocks.Application.Modularity
{
    public sealed class ModuleCatalog
    {
        private readonly IEnumerable<IModuleDefinition> _modules;

        public ModuleCatalog(IEnumerable<IModuleDefinition> modules)
            => _modules = modules;

        public IReadOnlyList<Assembly> GetApplicationAssemblies()
            => [.. _modules.Select(m => m.ApplicationAssembly).Distinct()];
    }
}
