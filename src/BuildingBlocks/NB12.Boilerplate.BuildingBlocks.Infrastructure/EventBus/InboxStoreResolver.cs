using Microsoft.Extensions.DependencyInjection;
using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.EventBus
{
    internal sealed class InboxStoreResolver(IServiceProvider sp) : IInboxStoreResolver
    {
        public IInboxStore Get(string moduleKey)
        {
            if (string.IsNullOrWhiteSpace(moduleKey))
                throw new ArgumentException("Module key must be provided.", nameof(moduleKey));

            var store = sp.GetKeyedService<IInboxStore>(moduleKey);

            if (store is null)
            {
                throw new InvalidOperationException(
                    $"No keyed IInboxStore registered for module '{moduleKey}'. " +
                    $"Register inbox store for this module (key='{moduleKey}') or disable InboxOptions.Enabled.");
            }

            return store;
        }
    }
}
