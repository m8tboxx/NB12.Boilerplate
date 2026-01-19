using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Auditing;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Persistence
{
    public static class DbContextRegistrationExtensions
    {
        public static IServiceCollection AddNpgsqlModuleDbContext<TDbContext>(
            this IServiceCollection services,
            IConfiguration config,
            string connectionStringName,
            string? migrationsAssembly = null,
            bool enableAuditing = true,
            Action<IServiceProvider, DbContextOptionsBuilder>? configureOptions = null)
            where TDbContext : DbContext
        {
            var cs = config.GetConnectionString(connectionStringName);
            if (string.IsNullOrWhiteSpace(cs))
                throw new InvalidOperationException($"Connectionstring '{connectionStringName}' is missing.");

            services.AddDbContext<TDbContext>((sp, opt) =>
            {
                opt.UseNpgsql(cs, npgsql =>
                {
                    if (!string.IsNullOrWhiteSpace(migrationsAssembly))
                        npgsql.MigrationsAssembly(migrationsAssembly);
                });

                if (enableAuditing)
                {
                    // Audit Interceptor (Created/Modified + AuditTrail)
                    opt.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
                }

                configureOptions?.Invoke(sp, opt);
            });

            return services;
        }
    }
}
