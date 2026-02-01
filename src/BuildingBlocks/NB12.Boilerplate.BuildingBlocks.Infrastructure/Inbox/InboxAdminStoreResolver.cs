using Microsoft.Extensions.DependencyInjection;
using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration.Admin;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Inbox
{
    internal sealed class InboxAdminStoreResolver(IServiceProvider sp) : IInboxAdminStoreResolver
    {
        public bool TryGet(string moduleKey, out IInboxAdminStore store)
        {
            store = sp.GetKeyedService<IInboxAdminStore>(moduleKey)!;
            return store is not null;
        }

        public IInboxAdminStore GetRequired(string moduleKey)
            => TryGet(moduleKey, out var store)
                ? store
                : throw new KeyNotFoundException($"No inbox admin store registered for module '{moduleKey}'.");
    }
}
