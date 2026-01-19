using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace NB12.Boilerplate.BuildingBlocks.Application.Modularity
{
    public interface IModuleDefinition
    {
        string Name { get; }
        Assembly ApplicationAssembly { get; }
        void AddModule(IServiceCollection services, IConfiguration config);
    }
}
