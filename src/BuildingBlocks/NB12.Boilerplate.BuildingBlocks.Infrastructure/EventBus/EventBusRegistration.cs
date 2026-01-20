using Microsoft.Extensions.DependencyInjection;
using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration;
using System.Reflection;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.EventBus
{
    public static class EventBusRegistration
    {
        public static IServiceCollection AddEventBus(this IServiceCollection services, params Assembly[] assemblies)
        {
            services.AddSingleton<IEventBus, InMemoryEventBus>();

            foreach (var type in assemblies.SelectMany(a => a.DefinedTypes))
            {
                if (type.IsAbstract || type.IsInterface) continue;

                foreach (var iface in type.ImplementedInterfaces)
                {
                    if (!iface.IsGenericType) continue;

                    if (iface.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>))
                        services.AddTransient(iface, type);

                    if (iface.GetGenericTypeDefinition() == typeof(IDomainEventToIntegrationEventMapper))
                        services.AddSingleton(iface, type);
                }
            }

            return services;
        }
    }
}
    

