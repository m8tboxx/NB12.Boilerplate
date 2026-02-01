using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Outbox
{
    public static class OutboxServiceCollectionExtensions
    {
        public static IServiceCollection AddOutboxForModule<TDbContext>(
            this IServiceCollection services,
            string moduleKey)
            where TDbContext : DbContext
        {
            if (string.IsNullOrWhiteSpace(moduleKey))
                throw new ArgumentException("Module key must be provided.", nameof(moduleKey));

            // Store (producer-side)
            services.AddSingleton<IModuleOutboxStore>(sp =>
                new EfCoreOutboxStore<TDbContext>(
                    sp.GetRequiredService<IDbContextFactory<TDbContext>>(), 
                    moduleKey));

            return services;
        }

        public static IServiceCollection AddOutboxCleanupForModule<TDbContext>(
            this IServiceCollection services,
            string moduleKey,
            IConfiguration configuration)
            where TDbContext : DbContext
        {
            if (string.IsNullOrWhiteSpace(moduleKey))
                throw new ArgumentException("Module key must be provided.", nameof(moduleKey));

            // Named options: global + per-module override
            services.AddOptions<OutboxCleanupOptions>(moduleKey)
                .Configure<IConfiguration>((opt, cfg) =>
                {
                    cfg.GetSection("OutboxCleanup").Bind(opt);
                    cfg.GetSection($"OutboxCleanup:Modules:{moduleKey}").Bind(opt);
                });

            // HostedService nur registrieren, wenn es (global oder per Modul) wirklich aktiv ist
            var globalEnabled = configuration.GetSection("OutboxCleanup").GetValue<bool>("Enabled");
            var moduleEnabled = configuration.GetSection($"OutboxCleanup:Modules:{moduleKey}").GetValue<bool?>("Enabled");

            if (!(moduleEnabled ?? globalEnabled))
                return services;

            services.AddHostedService(sp =>
            {
                var factory = sp.GetRequiredService<IDbContextFactory<TDbContext>>();
                var opts = sp.GetRequiredService<IOptionsMonitor<OutboxCleanupOptions>>();
                var logger = sp.GetRequiredService<ILogger<OutboxCleanupHostedService<TDbContext>>>();

                return new OutboxCleanupHostedService<TDbContext>(factory, opts, moduleKey, logger);
            });

            return services;
        }
    }
}
