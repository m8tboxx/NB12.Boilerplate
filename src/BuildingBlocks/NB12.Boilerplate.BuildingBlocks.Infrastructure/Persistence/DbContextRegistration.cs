using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Persistence
{
    public static class DbContextRegistration
    {
        /// <summary>
        /// Registers:
        /// - IDbContextFactory<TContext> as scoped (so it may use scoped services in configuration, e.g. interceptors)
        /// - TContext itself as scoped, created once per scope via the factory (Identity/UoW friendly)
        /// </summary>
        public static IServiceCollection AddNpgsqlDbContextFactoryAndScopedContext<TContext>(
            this IServiceCollection services,
            string connectionString,
            Action<IServiceProvider, DbContextOptionsBuilder> configure)
            where TContext : DbContext
        {
            services.AddDbContextFactory<TContext>((sp, opt) =>
            {
                opt.UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(TContext).Assembly.FullName);
                    npgsql.EnableRetryOnFailure(5);
                });

                configure(sp, opt);
            }, ServiceLifetime.Singleton);

            // This makes TContext available as a normal scoped DbContext (e.g. for Identity/UoW),
            // but it is created from the factory (one instance per scope).
            services.AddScoped<TContext>(sp =>
                sp.GetRequiredService<IDbContextFactory<TContext>>().CreateDbContext());

            return services;
        }
    }
}
