using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration.Admin;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Outbox
{
    public static class OutboxAdminServiceCollectionExtensions
    {
        public static IServiceCollection AddOutboxAdminForModule<TDbContext>(
            this IServiceCollection services,
            string moduleKey)
            where TDbContext : DbContext
        {
            if (string.IsNullOrWhiteSpace(moduleKey))
                throw new ArgumentException("Module key must be provided.", nameof(moduleKey));

            services.AddKeyedScoped<IOutboxAdminStore>(moduleKey, (sp, _) =>
                new EfCoreOutboxAdminStore<TDbContext>(sp.GetRequiredService<IDbContextFactory<TDbContext>>()));

            services.AddSingleton<IOutboxAdminModule>(new OutboxAdminModule(moduleKey));

            return services;
        }

        private sealed record OutboxAdminModule(string ModuleKey) : IOutboxAdminModule;
    }
}
