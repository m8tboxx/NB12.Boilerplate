using Microsoft.Extensions.DependencyInjection;
using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Inbox
{
    public sealed class InboxStoreResolver(IServiceProvider serviceProvider) : IInboxStoreResolver
    {
        public IInboxStore Get(string moduleKey)
            => serviceProvider.GetRequiredKeyedService<IInboxStore>(moduleKey);
    }
}
