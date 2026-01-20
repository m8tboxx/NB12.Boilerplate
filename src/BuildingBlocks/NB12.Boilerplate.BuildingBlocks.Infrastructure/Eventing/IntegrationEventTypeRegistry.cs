using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration;
using System.Collections.Concurrent;
using System.Reflection;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Eventing
{
    public sealed class IntegrationEventTypeRegistry
    {
        private readonly ConcurrentDictionary<string, Type> _map = new();

        public IntegrationEventTypeRegistry(params Assembly[] assemblies)
        {
            foreach (var t in assemblies.SelectMany(a => a.DefinedTypes))
            {
                if (t.IsAbstract || t.IsInterface) continue;
                if (!typeof(IIntegrationEvent).IsAssignableFrom(t)) continue;

                var key = t.FullName ?? t.Name;
                _map[key] = t.AsType();
            }
        }

        public bool TryResolve(string type, out Type resolved)
            => _map.TryGetValue(type, out resolved!);
    }
}
