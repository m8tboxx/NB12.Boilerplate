using Microsoft.Extensions.DependencyInjection;
using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration.Admin;
using System;
using System.Collections.Generic;
using System.Text;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Outbox
{
    internal sealed class OutboxAdminStoreResolver(IServiceProvider sp) : IOutboxAdminStoreResolver
    {
        public bool TryGet(string moduleKey, out IOutboxAdminStore store)
        {
            store = sp.GetKeyedService<IOutboxAdminStore>(moduleKey)!;
            return store is not null;
        }

        public IOutboxAdminStore GetRequired(string moduleKey)
            => TryGet(moduleKey, out var store)
                ? store
                : throw new KeyNotFoundException($"No outbox admin store registered for module '{moduleKey}'.");
    }
}
