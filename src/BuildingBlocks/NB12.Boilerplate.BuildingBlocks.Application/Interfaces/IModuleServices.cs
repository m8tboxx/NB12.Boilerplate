using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace NB12.Boilerplate.BuildingBlocks.Application.Interfaces
{
    public interface IModuleServices
    {
        string Name { get; }
        Assembly ApplicationAssembly { get; }
        void AddModule(IServiceCollection services, IConfiguration config);
    }
}
