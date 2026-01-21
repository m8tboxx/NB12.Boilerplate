using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            });

            services.AddScoped<IAuditReadRepository, AuditReadRepository>();
            services.AddScoped<IAuditLogWriter, EfCoreAuditLogWriter>();
            return services;
        }
    }
}
