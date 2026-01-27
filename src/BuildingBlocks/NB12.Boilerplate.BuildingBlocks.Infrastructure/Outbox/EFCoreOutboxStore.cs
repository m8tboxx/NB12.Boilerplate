using Microsoft.EntityFrameworkCore;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Ids;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Outbox
{
    public sealed class EfCoreOutboxStore<TDbContext>(TDbContext db, string module) : IModuleOutboxStore
    where TDbContext : DbContext
    {
        public string Module { get; } = module;

        public async Task<IReadOnlyList<OutboxMessage>> ClaimUnprocessed(
            int take,
            string lockOwner,
            TimeSpan lockTtl,
            CancellationToken ct)
        {
            take = Math.Max(1, take);

            if (string.IsNullOrWhiteSpace(lockOwner))
                throw new ArgumentException("Lock owner must be provided.", nameof(lockOwner));

            var now = DateTimeOffset.UtcNow;
            var lockedUntilUtc = now.Add(lockTtl);

            var table = GetQualifiedTableName(db);

            var sql = $@"
                WITH cte AS (
                    SELECT ""Id""
                    FROM {table}
                    WHERE ""ProcessedAtUtc"" IS NULL
                      AND (""LockedUntilUtc"" IS NULL OR ""LockedUntilUtc"" < {{0}})
                    ORDER BY ""OccurredAtUtc""
                    LIMIT {{1}}
                    FOR UPDATE SKIP LOCKED
                )
                UPDATE {table} AS m
                SET ""LockedUntilUtc"" = {{2}},
                    ""LockedBy"" = {{3}}
                FROM cte
                WHERE m.""Id"" = cte.""Id""
                RETURNING m.*;";

            return await db.Set<OutboxMessage>()
                .FromSqlRaw(sql, now, take, lockedUntilUtc, lockOwner)
                .ToListAsync(ct);
        }

        public async Task MarkProcessed(OutboxMessageId id, string lockOwner, DateTime utcNow, CancellationToken ct)
        {
            var table = GetQualifiedTableName(db);

            var sql = $@"
                UPDATE {table}
                SET ""ProcessedAtUtc"" = {{0}},
                    ""LockedUntilUtc"" = NULL,
                    ""LockedBy"" = NULL
                WHERE ""Id"" = {{1}}
                  AND ""LockedBy"" = {{2}}
                  AND ""ProcessedAtUtc"" IS NULL;";

            await db.Database.ExecuteSqlRawAsync(sql, new object[] { utcNow, id.Value, lockOwner }, ct);
        }

        public async Task MarkFailed(OutboxMessageId id, string lockOwner, DateTime utcNow, Exception ex, OutboxFailurePlan plan, CancellationToken ct)
        {
            var table = GetQualifiedTableName(db);

            if (plan.Action == OutboxFailureAction.DeadLetter)
            {
                var reason = plan.DeadLetterReason ?? "deadlettered";

                var sql = $@"
                    UPDATE {table}
                    SET ""AttemptCount"" = ""AttemptCount"" + 1,
                        ""LastError"" = {{0}},
                        ""DeadLetteredAtUtc"" = {{1}},
                        ""DeadLetterReason"" = {{2}},
                        ""LockedUntilUtc"" = NULL,
                        ""LockedBy"" = NULL
                    WHERE ""Id"" = {{3}}
                      AND ""LockedBy"" = {{4}}
                      AND ""ProcessedAtUtc"" IS NULL
                      AND ""DeadLetteredAtUtc"" IS NULL;";

                await db.Database.ExecuteSqlRawAsync(sql, new object[] { ex.ToString(), reason, id.Value, lockOwner }, ct);

                return;
            }


            if (plan.NextVisibleAtUtc is null)
                throw new ArgumentException("Retry plan requires NextVisibleAtUtc.", nameof(plan));

            var nextVisibleAtUtc = plan.NextVisibleAtUtc.Value;
            var nextVisibleAt = new DateTimeOffset(nextVisibleAtUtc);

            var retrySql = $@"
                UPDATE {table}
                SET ""AttemptCount"" = ""AttemptCount"" + 1,
                    ""LastError"" = {{0}},
                    ""LockedUntilUtc"" = {{1}},
                    ""LockedBy"" = NULL
                WHERE ""Id"" = {{2}}
                  AND ""LockedBy"" = {{3}}
                  AND ""ProcessedAtUtc"" IS NULL
                  AND ""DeadLetteredAtUtc"" IS NULL;";

            await db.Database.ExecuteSqlRawAsync(retrySql, new object[] { ex.ToString(), nextVisibleAt, id.Value, lockOwner }, ct);
        }


        private static string GetQualifiedTableName(DbContext db)
        {
            var entityType = db.Model.FindEntityType(typeof(OutboxMessage))
                ?? throw new InvalidOperationException("OutboxMessage is not part of the current DbContext model.");

            var table = entityType.GetTableName()
                ?? throw new InvalidOperationException("OutboxMessage table name could not be resolved.");

            var schema = entityType.GetSchema();

            static string Q(string ident) => "\"" + ident.Replace("\"", "\"\"") + "\"";

            return string.IsNullOrWhiteSpace(schema)
                ? Q(table)
                : $"{Q(schema)}.{Q(table)}";
        }
    }
}
