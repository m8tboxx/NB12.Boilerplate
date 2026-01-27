using Microsoft.EntityFrameworkCore;
using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration;
using NB12.Boilerplate.Modules.Audit.Infrastructure.Persistence;

namespace NB12.Boilerplate.Modules.Audit.Infrastructure.Inbox
{
    internal sealed class EfCoreInboxStore(AuditDbContext db) : IInboxStore
    {
        public async Task<bool> TryAcquireAsync(
            Guid integrationEventId,
            string handlerName,
            string lockOwner,
            DateTime utcNow,
            DateTime lockedUntilUtc,
            CancellationToken ct)
        {
            if (integrationEventId == Guid.Empty)
                throw new ArgumentException("Integration event id must be provided.", nameof(integrationEventId));

            if (string.IsNullOrWhiteSpace(handlerName))
                throw new ArgumentException("Handler name must be provided.", nameof(handlerName));

            if (string.IsNullOrWhiteSpace(lockOwner))
                throw new ArgumentException("Lock owner must be provided.", nameof(lockOwner));

            var table = GetQualifiedTableName(db);

            // Ensure row exists
            var insertSql = $@"
                INSERT INTO {table} (""Id"", ""IntegrationEventId"", ""HandlerName"", ""ReceivedAtUtc"", ""AttemptCount"", ""EventType"", ""PayloadJson"")
                VALUES ({{0}}, {{1}}, {{2}}, {{3}}, 0, {{4}}, CAST({{5}} AS jsonb))
                ON CONFLICT (""IntegrationEventId"", ""HandlerName"") DO NOTHING;";

            await db.Database.ExecuteSqlRawAsync(insertSql, new object[] { Guid.NewGuid(), integrationEventId, handlerName, utcNow, string.Empty, "{ }" }, ct);

            // Claim if not processed and not locked (or lock expired)
            var updateSql = $@"
                UPDATE {table}
                SET ""LockedUntilUtc"" = {{0}},
                    ""LockedOwner"" = {{1}},
                    ""AttemptCount"" = ""AttemptCount"" + 1,
                    ""LastError"" = NULL
                WHERE ""IntegrationEventId"" = {{2}}
                  AND ""HandlerName"" = {{3}}
                  AND ""ProcessedAtUtc"" IS NULL
                  AND (""LockedUntilUtc"" IS NULL OR ""LockedUntilUtc"" < {{4}});";

            var affected = await db.Database.ExecuteSqlRawAsync(
                updateSql,
                new object[] { lockedUntilUtc, lockOwner, integrationEventId, handlerName, utcNow },
                ct);

            return affected == 1;
        }

        public async Task MarkProcessedAsync(
            Guid integrationEventId,
            string handlerName,
            string lockOwner,
            DateTime processedAtUtc,
            CancellationToken ct)
        {
            var table = GetQualifiedTableName(db);

            var sql = $@"
                UPDATE {table}
                SET ""ProcessedAtUtc"" = {{0}},
                    ""LockedUntilUtc"" = NULL,
                    ""LockedOwner"" = NULL
                WHERE ""IntegrationEventId"" = {{1}}
                  AND ""HandlerName"" = {{2}}
                  AND ""LockedOwner"" = {{3}}
                  AND ""ProcessedAtUtc"" IS NULL;";

            await db.Database.ExecuteSqlRawAsync(sql, new object[] { processedAtUtc, integrationEventId, handlerName, lockOwner }, ct);
        }

        public async Task MarkFailedAsync(
            Guid integrationEventId,
            string handlerName,
            string lockOwner,
            DateTime failedAtUtc,
            string error,
            CancellationToken ct)
        {
            var table = GetQualifiedTableName(db);

            var sql = $@"
                UPDATE {table}
                SET ""LastError"" = {{0}},
                    ""LastFailedAtUtc"" = {{1}},
                    ""LockedUntilUtc"" = NULL,
                    ""LockedOwner"" = NULL
                WHERE ""IntegrationEventId"" = {{2}}
                  AND ""HandlerName"" = {{3}}
                  AND ""LockedOwner"" = {{4}}
                  AND ""ProcessedAtUtc"" IS NULL;";

            await db.Database.ExecuteSqlRawAsync(sql, new object[] { error, failedAtUtc, integrationEventId, handlerName, lockOwner }, ct);
        }

        private static string GetQualifiedTableName(DbContext db)
        {
            var entityType = db.Model.FindEntityType(typeof(InboxMessage))
                ?? throw new InvalidOperationException("InboxMessage is not part of the current DbContext model.");

            var table = entityType.GetTableName()
                ?? throw new InvalidOperationException("InboxMessage table name could not be resolved.");

            var schema = entityType.GetSchema();

            static string Q(string ident) => "\"" + ident.Replace("\"", "\"\"") + "\"";

            return string.IsNullOrWhiteSpace(schema)
                ? Q(table)
                : $"{Q(schema)}.{Q(table)}";
        }
    }
}
