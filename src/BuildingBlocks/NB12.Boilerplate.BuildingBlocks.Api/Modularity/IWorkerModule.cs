using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NB12.Boilerplate.BuildingBlocks.Api.Modularity
{
    /// <summary>
    /// Worker-only registrations for a module (HostedServices, polling, cleanup, etc.).
    /// Host.Worker calls this explicitly; Host.API must not.
    /// </summary>
    public interface IWorkerModule
    {
        void AddWorker(IServiceCollection services, IConfiguration config);
    }
}
