using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Inbox
{
    public static class InboxServiceCollectionExtensions
    {
        public static IServiceCollection AddInboxCore(this IServiceCollection services)
        {
            services.AddScoped<IInboxStoreResolver, InboxStoreResolver>();
            return services;
        }

        public static IServiceCollection AddInboxForModule<TDbContext>(
            this IServiceCollection services,
            string moduleKey,
            IConfiguration configuration)
            where TDbContext : DbContext
        {
            if (string.IsNullOrWhiteSpace(moduleKey))
                throw new ArgumentException("Module key must be provided.", nameof(moduleKey));

            // Named options per module: global section + per-module override section.
            services.AddOptions<InboxMonitoringOptions>(moduleKey)
                .Configure<IConfiguration>((opt, cfg) =>
                {
                    cfg.GetSection("InboxMonitoring").Bind(opt);
                    cfg.GetSection($"InboxMonitoring:Modules:{moduleKey}").Bind(opt);
                });

            services.AddOptions<InboxCleanupOptions>(moduleKey)
                .Configure<IConfiguration>((opt, cfg) =>
                {
                    cfg.GetSection("InboxCleanup").Bind(opt);
                    cfg.GetSection($"InboxCleanup:Modules:{moduleKey}").Bind(opt);
                });

            // Keyed inbox store for this module
            //services.AddKeyedSingleton<IInboxStore, EfCoreInboxStore<TDbContext>>(moduleKey);

            services.AddKeyedScoped<IInboxStore>(moduleKey, (sp, _) =>
                new EfCoreInboxStore<TDbContext>(sp.GetRequiredService<IDbContextFactory<TDbContext>>()));

            // Keyed stats state for this module + expose as module provider via IEnumerable later if needed
            services.AddKeyedSingleton(moduleKey, new ModuleInboxStatsState(moduleKey));

            // Hosted services per module
            services.AddHostedService(sp =>
            {
                var factory = sp.GetRequiredService<IDbContextFactory<TDbContext>>();
                var opts = sp.GetRequiredService<IOptions<InboxMonitoringOptions>>();
                var state = sp.GetRequiredKeyedService<ModuleInboxStatsState>(moduleKey);
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<InboxStatsCollectorHostedService<TDbContext>>>();
                return new InboxStatsCollectorHostedService<TDbContext>(factory, opts, state, logger);
            });

            services.AddHostedService(sp =>
            {
                var factory = sp.GetRequiredService<IDbContextFactory<TDbContext>>();
                var opts = sp.GetRequiredService<IOptions<InboxCleanupOptions>>();
                var state = sp.GetRequiredKeyedService<ModuleInboxStatsState>(moduleKey);
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<InboxCleanupHostedService<TDbContext>>>();
                return new InboxCleanupHostedService<TDbContext>(factory, opts, state, logger);
            });

            return services;
        }
    }
}
