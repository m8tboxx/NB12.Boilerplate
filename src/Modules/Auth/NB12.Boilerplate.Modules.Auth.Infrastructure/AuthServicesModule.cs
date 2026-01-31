using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NB12.Boilerplate.BuildingBlocks.Api.Modularity;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Outbox;
using NB12.Boilerplate.Modules.Auth.Application;
using NB12.Boilerplate.Modules.Auth.Contracts.IntegrationEvents;
using NB12.Boilerplate.Modules.Auth.Infrastructure.Constants;
using NB12.Boilerplate.Modules.Auth.Infrastructure.Persistence;
using System.Reflection;

namespace NB12.Boilerplate.Modules.Auth.Infrastructure
{
    public sealed class AuthServicesModule : IServiceModule, IModuleAssemblyProvider, IModuleKeyProvider
    {
        public string Name => "AuthModule";
        public string ModuleKey => AuthModule.Key;
        public Assembly ApplicationAssembly => typeof(AssemblyMarker).Assembly;

        
        public void AddModule(IServiceCollection services, IConfiguration config)
        {
            services.AddAuthInfrastructure(config);
        }


        public void AddWorker(IServiceCollection services, IConfiguration configuration)
        {
            services.AddOutboxCleanupForModule<AuthDbContext>(ModuleKey, configuration);
        }


        public IEnumerable<Assembly> GetAdditionalAssemblies()
            => new[]
            {
                typeof(AssemblyMarker).Assembly,
                typeof(UserCreatedIntegrationEvent).Assembly,
            };
    }
}
