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

            foreach (var typeInfo in assemblies.SelectMany(a => a.DefinedTypes))
            {
                if (typeInfo.IsAbstract || typeInfo.IsInterface)
                    continue;

                var type = typeInfo.AsType();

                // Non-generic mapper interface
                if (typeof(IDomainEventToIntegrationEventMapper).IsAssignableFrom(type))
                {
                    if (!services.Any(sd => sd.ServiceType == typeof(IDomainEventToIntegrationEventMapper) && sd.ImplementationType == type))
                    {
                        services.AddSingleton(typeof(IDomainEventToIntegrationEventMapper), type);
                    }
                }

                // Generic integration event handlers
                foreach (var iface in typeInfo.ImplementedInterfaces)
                {
                    if (!iface.IsGenericType) continue;

                    if (iface.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>))
                    {
                        services.AddTransient(iface, type);
                    }
                }
            }

            return services;
        }
    }
}
    

