using System.Reflection;

namespace NB12.Boilerplate.BuildingBlocks.Api.Modularity
{
    /// <summary>
    /// Optional extension point for modules to provide additional assemblies
    /// that must be included for scanning (e.g. Application, Contracts).
    /// </summary>
    public interface IModuleAssemblyProvider
    {
        IEnumerable<Assembly> GetAdditionalAssemblies();
    }
}
