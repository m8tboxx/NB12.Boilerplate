using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NB12.Boilerplate.BuildingBlocks.Application.Interfaces;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Inbox;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Persistence;
using NB12.Boilerplate.Modules.Audit.Application.Interfaces;
using NB12.Boilerplate.Modules.Audit.Application.Options;
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

            services.AddNpgsqlDbContextFactoryAndScopedContext<AuditDbContext>(
                cs,
                configure: (_, __) => { });

            services.AddScoped<IAuditReadRepository, AuditReadRepository>();
            services.AddScoped<IAuditLogWriter, EfCoreAuditLogWriter>();
            services.AddScoped<IErrorAuditWriter, EfCoreErrorLogWriter>();

            // Inbox Admin
            services.AddScoped<IInboxAdminRepository, InboxAdminRepository>();
            services.AddInboxForModule<AuditDbContext>("Audit", config);

            // Retention
            services.AddOptions<AuditRetentionOptions>()
                .Bind(config.GetSection(AuditRetentionOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddSingleton<AuditRetentionStatusState>();
            services.AddScoped<IAuditRetentionConfigProvider, AuditRetentionConfigProvider>();
            services.AddScoped<IAuditRetentionStatusProvider, AuditRetentionStatusProvider>();
            services.AddScoped<IAuditRetentionService, AuditRetentionService>();

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
