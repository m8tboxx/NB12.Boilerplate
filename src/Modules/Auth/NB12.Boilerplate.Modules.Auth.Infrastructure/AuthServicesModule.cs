using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NB12.Boilerplate.BuildingBlocks.Api.Modularity;
using NB12.Boilerplate.Modules.Auth.Application;
using NB12.Boilerplate.Modules.Auth.Contracts.IntegrationEvents;
using System.Reflection;

namespace NB12.Boilerplate.Modules.Auth.Infrastructure
{
    public sealed class AuthServicesModule : IServiceModule, IModuleAssemblyProvider
    {
        public string Name => "AuthModule";
        public Assembly ApplicationAssembly => typeof(AssemblyMarker).Assembly;

        public void AddModule(IServiceCollection services, IConfiguration config)
        {
            services.AddAuthInfrastructure(config);
        }

        public IEnumerable<Assembly> GetAdditionalAssemblies()
            => new[]
            {
                typeof(AssemblyMarker).Assembly,
                typeof(UserCreatedIntegrationEvent).Assembly, // Contracts
            };
    }
}
