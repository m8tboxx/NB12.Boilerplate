using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NB12.Boilerplate.BuildingBlocks.Api.Modularity;
using NB12.Boilerplate.Modules.Auth.Api.Cookies;
using NB12.Boilerplate.Modules.Auth.Application;
using System.Reflection;

namespace NB12.Boilerplate.Modules.Auth.Api
{
    public sealed class AuthApiServicesModule : IServiceModule
    {
        public string Name => "AuthApi";
        public Assembly ApplicationAssembly => typeof(AssemblyMarker).Assembly;

        public void AddModule(IServiceCollection services, IConfiguration config)
        {
            services.AddScoped<RefreshTokenCookies>();
        }
    }
}
