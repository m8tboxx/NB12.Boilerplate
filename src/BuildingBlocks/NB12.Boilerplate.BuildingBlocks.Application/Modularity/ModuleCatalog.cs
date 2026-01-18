using NB12.Boilerplate.BuildingBlocks.Application.Interfaces;
using System.Reflection;

namespace NB12.Boilerplate.BuildingBlocks.Application.Modularity
{
    public static class ModuleCatalog
    {
        public static Assembly[] GetApplicationAssemblies(IEnumerable<IModuleServices> modules)
            => [.. modules.Select(m => m.ApplicationAssembly).Distinct()];
    }
}
