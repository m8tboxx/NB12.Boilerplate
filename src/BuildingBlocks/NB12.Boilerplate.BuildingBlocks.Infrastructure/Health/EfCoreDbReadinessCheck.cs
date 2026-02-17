using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NB12.Boilerplate.BuildingBlocks.Application.Health;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Health
{
    internal sealed class EfCoreDbReadinessCheck<TDbContext>(
        string name,
        IDbContextFactory<TDbContext> factory,
        ILogger<EfCoreDbReadinessCheck<TDbContext>> logger)
        : IReadinessCheck
        where TDbContext : DbContext
    {
        public string Name => name;

        public async Task<ReadinessCheckResult> CheckAsync(CancellationToken ct)
        {
            // Hard timeout to protect the ready endpoint. Keep it short.
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(2));

            try
            {
                await using var db = await factory.CreateDbContextAsync(cts.Token);

                // CanConnect() is fast and works for both pooled + factory contexts.
                var ok = await db.Database.CanConnectAsync(cts.Token);
                if (!ok)
                    return ReadinessCheckResult.Unhealthy("Database.CanConnectAsync returned false.");

                // Stronger than CanConnect: forces a roundtrip.
                await db.Database.ExecuteSqlRawAsync("SELECT 1;", cts.Token);

                return ReadinessCheckResult.Healthy();
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                return ReadinessCheckResult.Unhealthy("Timeout while checking database connectivity.");
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Readiness DB check failed: {Name}", name);
                return ReadinessCheckResult.Unhealthy(ex.Message);
            }
        }
    }
}
