using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NB12.Boilerplate.BuildingBlocks.Application.Interfaces;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Eventing;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Outbox;
using NB12.Boilerplate.Modules.Audit.Application.Interfaces;
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

            services.AddDbContext<AuditDbContext>((sp, opt) =>
            {
                opt.UseNpgsql(cs, npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(AuditDbContext).Assembly.FullName);
                });

                opt.AddInterceptors(sp.GetRequiredService<DomainEventsOutboxInterceptor>());
            });

            // Überschreibt NoOpAuditStore
            services.AddScoped<IAuditStore, EFCoreAuditStore>();

            services.AddScoped<IAuditReadRepository, AuditReadRepository>();

            services.AddScoped<IModuleOutboxStore>(sp =>
                new EfCoreOutboxStore<AuditDbContext>(sp.GetRequiredService<AuditDbContext>(), module: "Audit"));

            return services;
        }
    }
}
