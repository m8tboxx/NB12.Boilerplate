using Microsoft.Extensions.DependencyInjection;
using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using System.Reflection;

namespace NB12.Boilerplate.BuildingBlocks.Application.Messaging
{
    public static class MessagingRegistration
    {
        public static IServiceCollection AddMessaging(this IServiceCollection services, params Assembly[] assemblies)
        {
            services.AddScoped<ISender, Sender>();

            foreach (var type in assemblies.SelectMany(a => a.DefinedTypes))
            {
                if (type.IsAbstract || type.IsInterface)
                    continue;

                foreach (var iface in type.ImplementedInterfaces)
                {
                    if (!iface.IsGenericType)
                        continue;

                    if (iface.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
                        services.AddTransient(iface, type);
                }
            }

            return services;
        }
    }
}
