using Microsoft.Extensions.DependencyInjection;
using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration;
using System.Reflection;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.EventBus
{
    public static class EventBusRegistration
    {
        public static IServiceCollection AddEventBus(this IServiceCollection services, params Assembly[] assemblies)
        {
            services.AddOptions<InboxOptions>();

            if (!services.Any(sd => sd.ServiceType == typeof(IInboxStore)))
                services.AddSingleton<IInboxStore, NoOpInboxStore>();

            if (!services.Any(sd => sd.ServiceType == typeof(IInboxStatsProvider)))
                services.AddSingleton<IInboxStatsProvider, NoOpInboxStatsProvider>();

            if (!services.Any(sd => sd.ServiceType == typeof(InboxMetrics)))
                services.AddSingleton<InboxMetrics>();

            services.AddSingleton<IEventBus, InMemoryEventBus>();

            foreach (var typeInfo in assemblies.SelectMany(a => a.DefinedTypes))
            {
                if (typeInfo.IsAbstract || typeInfo.IsInterface)
                    continue;

                var type = typeInfo.AsType();

                if (typeof(IDomainEventToIntegrationEventMapper).IsAssignableFrom(type))
                {
                    if (!services.Any(sd => sd.ServiceType == typeof(IDomainEventToIntegrationEventMapper) && sd.ImplementationType == type))
                        services.AddSingleton(typeof(IDomainEventToIntegrationEventMapper), type);
                }

                foreach (var iface in typeInfo.ImplementedInterfaces)
                {
                    if (!iface.IsGenericType) continue;

                    if (iface.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>))
                        services.AddTransient(iface, type);
                }
            }

            return services;
        }
    }
}
    

