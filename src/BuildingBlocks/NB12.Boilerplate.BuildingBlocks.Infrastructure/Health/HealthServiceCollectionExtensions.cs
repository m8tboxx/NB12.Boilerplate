using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NB12.Boilerplate.BuildingBlocks.Application.Health;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Health
{
    public static class HealthServiceCollectionExtensions
    {
        /// <summary>
        /// Registers a readiness check that verifies database connectivity for the given DbContext factory.
        /// </summary>
        public static IServiceCollection AddEfCoreDbReadinessCheck<TDbContext>(
            this IServiceCollection services,
            string name)
            where TDbContext : DbContext
        {
            services.AddSingleton<IReadinessCheck>(sp =>
                new EfCoreDbReadinessCheck<TDbContext>(
                    name,
                    sp.GetRequiredService<IDbContextFactory<TDbContext>>(),
                    sp.GetRequiredService<ILogger<EfCoreDbReadinessCheck<TDbContext>>>()));

            return services;
        }
    }
}
