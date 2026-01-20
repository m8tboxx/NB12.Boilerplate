using Microsoft.Extensions.DependencyInjection;
using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Domain;
using System.Reflection;

namespace NB12.Boilerplate.BuildingBlocks.Application.Eventing
{
    public static class EventingRegistration
    {
        public static IServiceCollection AddDomainEventing(this IServiceCollection services, params Assembly[] assemblies)
        {
            services.AddSingleton<IDomainEventDispatcher, InProcessDomainEventDispatcher>();

            foreach (var type in assemblies.SelectMany(a => a.DefinedTypes))
            {
                if (type.IsAbstract || type.IsInterface) continue;

                foreach (var iface in type.ImplementedInterfaces)
                {
                    if (!iface.IsGenericType) continue;

                    if (iface.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>))
                        services.AddTransient(iface, type);
                }
            }

            return services;
        }
    }
}
