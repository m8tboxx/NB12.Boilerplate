using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NB12.Boilerplate.BuildingBlocks.Web.Modularity;
using NB12.Boilerplate.Modules.Auth.Application;
using System.Reflection;

namespace NB12.Boilerplate.Modules.Auth.Infrastructure
{
    public sealed class AuthServicesModule : IModuleServices
    {
        public string Name => "AuthModule";
        public Assembly ApplicationAssembly => typeof(AssemblyMarker).Assembly;

        public void AddModule(IServiceCollection services, IConfiguration config)
        {
            services.AddAuthInfrastructure(config);
        }
    }
}
