using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NB12.Boilerplate.BuildingBlocks.Application.Interfaces;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Inbox;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Persistence;
using NB12.Boilerplate.Modules.Audit.Application.Interfaces;
using NB12.Boilerplate.Modules.Audit.Application.Options;
using NB12.Boilerplate.Modules.Audit.Contracts.Auditing;
using NB12.Boilerplate.Modules.Audit.Infrastructure.Constants;
using NB12.Boilerplate.Modules.Audit.Infrastructure.Persistence;
using NB12.Boilerplate.Modules.Audit.Infrastructure.Repositories;
using NB12.Boilerplate.Modules.Audit.Infrastructure.Services;

namespace NB12.Boilerplate.Modules.Audit.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddAuditInfrastructure(this IServiceCollection services, IConfiguration config)
        {
            var connetctionString = config.GetConnectionString("AuditDb");
            if (string.IsNullOrWhiteSpace(connetctionString))
                throw new InvalidOperationException("Connectionstring 'AuditDb' is missing");

            services.AddNpgsqlDbContextFactoryAndScopedContext<AuditDbContext>(
                connetctionString,
                configure: (_, __) => { });

            services.AddScoped<IAuditReadRepository, AuditReadRepository>();
            services.AddScoped<IAuditLogWriter, EfCoreAuditLogWriter>();
            services.AddScoped<IErrorAuditWriter, EfCoreErrorLogWriter>();

            // Inbox Admin
            services.AddInboxForModule<AuditDbContext>(AuditModule.Key, config);
            services.AddInboxAdminForModule<AuditDbContext>(AuditModule.Key);

            // Retention
            services.AddOptions<AuditRetentionOptions>()
                .Bind(config.GetSection(AuditRetentionOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddSingleton<AuditRetentionStatusState>();
            services.AddScoped<IAuditRetentionConfigProvider, AuditRetentionConfigProvider>();
            services.AddScoped<IAuditRetentionStatusProvider, AuditRetentionStatusProvider>();
            services.AddScoped<IAuditRetentionService, AuditRetentionService>();

            services.AddSingleton<IAuditIntegrationEventFactory, AuditIntegrationEventFactory>();

            return services;
        }
    }
}
