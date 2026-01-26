using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration;
using NB12.Boilerplate.BuildingBlocks.Application.Interfaces;
using NB12.Boilerplate.Modules.Audit.Application.Interfaces;
using NB12.Boilerplate.Modules.Audit.Application.Options;
using NB12.Boilerplate.Modules.Audit.Infrastructure.Inbox;
using NB12.Boilerplate.Modules.Audit.Infrastructure.Persistence;
using NB12.Boilerplate.Modules.Audit.Infrastructure.Repositories;
using NB12.Boilerplate.Modules.Audit.Infrastructure.Services;

namespace NB12.Boilerplate.Modules.Audit.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddAuditInfrastructure(this IServiceCollection services, IConfiguration config)
        {
            var cs = config.GetConnectionString("AuditDb");
            if (string.IsNullOrWhiteSpace(cs))
                throw new InvalidOperationException("Connectionstring 'AuditDb' is missing");

            // IMPORTANT:
            // AddDbContextFactory is SINGLETON by default. That collides with scoped DbContextOptions from AddDbContext.
            // Fix: register factory as SCOPED, and register it BEFORE AddDbContext.
            services.AddDbContextFactory<AuditDbContext>(opt =>
            {
                opt.UseNpgsql(cs, npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(AuditDbContext).Assembly.FullName);
                });
            }, ServiceLifetime.Scoped);

            // Scoped DbContext (writes / retention / anything transactional inside a request scope)
            services.AddDbContext<AuditDbContext>((sp, opt) =>
            {
                opt.UseNpgsql(cs, npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(AuditDbContext).Assembly.FullName);
                });
            });

            services.AddScoped<IAuditReadRepository, AuditReadRepository>();
            services.AddScoped<IAuditLogWriter, EfCoreAuditLogWriter>();
            services.AddScoped<IErrorAuditWriter, EfCoreErrorLogWriter>();

            // Inbox Admin
            services.AddScoped<IInboxAdminRepository, InboxAdminRepository>();

            // Retention
            services.AddOptions<AuditRetentionOptions>()
                .Bind(config.GetSection(AuditRetentionOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddSingleton<AuditRetentionStatusState>();
            services.AddSingleton<IAuditRetentionStatusProvider>(sp => sp.GetRequiredService<AuditRetentionStatusState>());
            services.AddScoped<IAuditRetentionService, AuditRetentionService>();

            services.AddScoped<IInboxStore, EfCoreInboxStore>();

            // Stats provider for metrics (overrides NoOpInboxStatsProvider)
            services.AddSingleton<InboxStatsState>();
            services.AddSingleton<IInboxStatsProvider>(sp => sp.GetRequiredService<InboxStatsState>());

            // Cleanup + Monitoring options
            services.Configure<InboxCleanupOptions>(config.GetSection("InboxCleanup"));
            services.Configure<InboxMonitoringOptions>(config.GetSection("InboxMonitoring"));

            // Hosted services: only enabled if config says so (so API host can keep them off)
            if (config.GetSection("InboxMonitoring").GetValue<bool>("Enabled"))
                services.AddHostedService<InboxStatsCollectorHostedService>();

            if (config.GetSection("InboxCleanup").GetValue<bool>("Enabled"))
                services.AddHostedService<InboxCleanupHostedService>();

            // HostedService nur registrieren, wenn konfiguriert (sonst läuft er sinnlos im API Host)
            var retentionEnabled = config.GetSection(AuditRetentionOptions.SectionName).GetValue<bool>("Enabled");
            if (retentionEnabled)
            {
                services.AddHostedService<AuditRetentionHostedService>();
            }

            return services;
        }
    }
}
