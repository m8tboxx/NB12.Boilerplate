using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration.Admin;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Inbox
{
    public static class InboxAdminServiceCollectionExtensions
    {
        public static IServiceCollection AddInboxAdminForModule<TDbContext>(
            this IServiceCollection services,
            string moduleKey)
            where TDbContext : DbContext
        {
            if (string.IsNullOrWhiteSpace(moduleKey))
                throw new ArgumentException("Module key must be provided.", nameof(moduleKey));

            services.AddKeyedScoped<IInboxAdminStore>(moduleKey, (sp, _) =>
                new EfCoreInboxAdminStore<TDbContext>(sp.GetRequiredService<IDbContextFactory<TDbContext>>()));

            services.AddSingleton<IInboxAdminModule>(new InboxAdminModule(moduleKey));

            return services;
        }

        private sealed record InboxAdminModule(string ModuleKey) : IInboxAdminModule;
    }
}
