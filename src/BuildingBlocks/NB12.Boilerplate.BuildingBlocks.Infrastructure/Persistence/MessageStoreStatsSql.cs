using Microsoft.EntityFrameworkCore;
using System.Data;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Persistence
{
    public static class MessageStoreStatsSql
    {
        public static async Task<MessageStoreStats> GetMessageStoreStatsAsync<TEntity>(
            this DbContext db,
            DateTime nowUtc,
            string timestampPropertyName,
            CancellationToken ct)
        {
            var (table, storeId, et) = EfPostgresSql.Table<TEntity>(db);

            var ts = EfPostgresSql.Column(et, storeId, timestampPropertyName);
            var processedAt = EfPostgresSql.Column(et, storeId, "ProcessedAtUtc");
            var attemptCount = EfPostgresSql.Column(et, storeId, "AttemptCount");

            var hasLockedUntil = EfPostgresSql.HasProperty(et, "LockedUntilUtc");
            var lockedExpr = hasLockedUntil
                ? $"COUNT(*) FILTER (WHERE {processedAt} IS NULL AND {EfPostgresSql.Column(et, storeId, "LockedUntilUtc")} IS NOT NULL AND {EfPostgresSql.Column(et, storeId, "LockedUntilUtc")} > @now)"
                : "0";

            var sql = $@"
                SELECT
                    COUNT(*)::bigint                                                        AS total,
                    COUNT(*) FILTER (WHERE {processedAt} IS NOT NULL)::bigint                AS processed,
                    COUNT(*) FILTER (WHERE {processedAt} IS NULL AND {attemptCount} = 0)::bigint AS pending,
                    COUNT(*) FILTER (WHERE {processedAt} IS NULL AND {attemptCount} > 0)::bigint AS failed,
                    ({lockedExpr})::bigint                                                   AS locked,
                    MIN({ts}) FILTER (WHERE {processedAt} IS NULL)                            AS oldest_pending,
                    MIN({ts}) FILTER (WHERE {processedAt} IS NULL AND {attemptCount} > 0)     AS oldest_failed
                FROM {table}";

            var conn = db.Database.GetDbConnection();
            var shouldClose = conn.State != ConnectionState.Open;

            if (shouldClose)
                await conn.OpenAsync(ct);

            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.CommandType = CommandType.Text;

                var pNow = cmd.CreateParameter();
                pNow.ParameterName = "now";
                pNow.Value = nowUtc;
                cmd.Parameters.Add(pNow);

                await using var reader = await cmd.ExecuteReaderAsync(ct);
                if (!await reader.ReadAsync(ct))
                    return new MessageStoreStats(0, 0, 0, 0, 0, null, null);

                var total = reader.GetInt64(0);
                var processed = reader.GetInt64(1);
                var pending = reader.GetInt64(2);
                var failed = reader.GetInt64(3);
                var locked = reader.GetInt64(4);

                DateTime? oldestPending = reader.IsDBNull(5) ? null : reader.GetFieldValue<DateTime>(5);
                DateTime? oldestFailed = reader.IsDBNull(6) ? null : reader.GetFieldValue<DateTime>(6);

                return new MessageStoreStats(
                    Total: total,
                    Pending: pending,
                    Failed: failed,
                    Processed: processed,
                    Locked: locked,
                    OldestPendingUtc: oldestPending,
                    OldestFailedUtc: oldestFailed);
            }
            finally
            {
                if (shouldClose)
                    await conn.CloseAsync();
            }
        }
    }
}
