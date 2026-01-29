using Microsoft.EntityFrameworkCore;
using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Persistence;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Inbox
{
    public sealed class EfCoreInboxStore<TDbContext>(IDbContextFactory<TDbContext> dbFactory) : IInboxStore
        where TDbContext : DbContext
    {
        public async Task<bool> TryAcquireAsync(
            Guid integrationEventId,
            string handlerName,
            string lockOwner,
            DateTime utcNow,
            DateTime lockedUntilUtc,
            string eventType,
            string payloadJson,
            CancellationToken ct)
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            if (integrationEventId == Guid.Empty)
                throw new ArgumentException("Integration event id must be provided.", nameof(integrationEventId));
            if (string.IsNullOrWhiteSpace(handlerName))
                throw new ArgumentException("Handler name must be provided.", nameof(handlerName));
            if (string.IsNullOrWhiteSpace(lockOwner))
                throw new ArgumentException("Lock owner must be provided.", nameof(lockOwner));
            if (string.IsNullOrWhiteSpace(eventType))
                throw new ArgumentException("Event type must be provided.", nameof(eventType));
            if (string.IsNullOrWhiteSpace(payloadJson))
                throw new ArgumentException("PayloadJson must be provided.", nameof(payloadJson));

            var (table, storeId, et) = EfPostgresSql.Table<InboxMessage>(db);

            var idCol = EfPostgresSql.Column(et, storeId, nameof(InboxMessage.Id));
            var eventIdCol = EfPostgresSql.Column(et, storeId, nameof(InboxMessage.IntegrationEventId));
            var handlerCol = EfPostgresSql.Column(et, storeId, nameof(InboxMessage.HandlerName));
            var receivedAtCol = EfPostgresSql.Column(et, storeId, nameof(InboxMessage.ReceivedAtUtc));
            var attemptCol = EfPostgresSql.Column(et, storeId, nameof(InboxMessage.AttemptCount));
            var typeCol = EfPostgresSql.Column(et, storeId, nameof(InboxMessage.EventType));
            var payloadCol = EfPostgresSql.Column(et, storeId, nameof(InboxMessage.PayloadJson));

            var processedAtCol = EfPostgresSql.Column(et, storeId, nameof(InboxMessage.ProcessedAtUtc));
            var lockedUntilCol = EfPostgresSql.Column(et, storeId, nameof(InboxMessage.LockedUntilUtc));
            var lockedOwnerCol = EfPostgresSql.Column(et, storeId, nameof(InboxMessage.LockedOwner));
            var deadLetteredAtCol = EfPostgresSql.Column(et, storeId, nameof(InboxMessage.DeadLetteredAtUtc));

            // Ensure row exists. ReceivedAtUtc stays from first insert.
            var insertSql = $@"
                INSERT INTO {table} ({idCol}, {eventIdCol}, {handlerCol}, {receivedAtCol}, {attemptCol}, {typeCol}, {payloadCol})
                VALUES ({{0}}, {{1}}, {{2}}, {{3}}, 0, {{4}}, CAST({{5}} AS jsonb))
                ON CONFLICT ({eventIdCol}, {handlerCol}) DO UPDATE
                    SET {typeCol} = EXCLUDED.{typeCol},
                        {payloadCol} = EXCLUDED.{payloadCol};";

            await db.Database.ExecuteSqlRawAsync(
                insertSql,
                new object[] { Guid.NewGuid(), integrationEventId, handlerName, utcNow, eventType, payloadJson },
                ct);

            // Acquire lock (do NOT touch AttemptCount here!)
            var updateSql = $@"
                UPDATE {table}
                SET {lockedUntilCol} = {{0}},
                    {lockedOwnerCol} = {{1}}
                WHERE {eventIdCol} = {{2}}
                  AND {handlerCol} = {{3}}
                  AND {processedAtCol} IS NULL
                  AND {deadLetteredAtCol} IS NULL
                  AND ({lockedUntilCol} IS NULL OR {lockedUntilCol} < {{4}});";

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
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            var (table, storeId, et) = EfPostgresSql.Table<InboxMessage>(db);

            var eventIdCol = EfPostgresSql.Column(et, storeId, nameof(InboxMessage.IntegrationEventId));
            var handlerCol = EfPostgresSql.Column(et, storeId, nameof(InboxMessage.HandlerName));
            var processedAtCol = EfPostgresSql.Column(et, storeId, nameof(InboxMessage.ProcessedAtUtc));
            var lockedUntilCol = EfPostgresSql.Column(et, storeId, nameof(InboxMessage.LockedUntilUtc));
            var lockedOwnerCol = EfPostgresSql.Column(et, storeId, nameof(InboxMessage.LockedOwner));
            var lastErrorCol = EfPostgresSql.Column(et, storeId, nameof(InboxMessage.LastError));
            var lastFailedAtCol = EfPostgresSql.Column(et, storeId, nameof(InboxMessage.LastFailedAtUtc));

            var sql = $@"
                UPDATE {table}
                SET {processedAtCol} = {{0}},
                    {lockedUntilCol} = NULL,
                    {lockedOwnerCol} = NULL,
                    {lastErrorCol} = NULL,
                    {lastFailedAtCol} = NULL
                WHERE {eventIdCol} = {{1}}
                  AND {handlerCol} = {{2}}
                  AND {lockedOwnerCol} = {{3}}
                  AND {processedAtCol} IS NULL;";

            await db.Database.ExecuteSqlRawAsync(
                sql,
                new object[] { processedAtUtc, integrationEventId, handlerName, lockOwner },
                ct);
        }

        public async Task MarkFailedAsync(
            Guid integrationEventId,
            string handlerName,
            string lockOwner,
            DateTime failedAtUtc,
            string error,
            CancellationToken ct)
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            var (table, storeId, et) = EfPostgresSql.Table<InboxMessage>(db);

            var eventIdCol = EfPostgresSql.Column(et, storeId, nameof(InboxMessage.IntegrationEventId));
            var handlerCol = EfPostgresSql.Column(et, storeId, nameof(InboxMessage.HandlerName));
            var processedAtCol = EfPostgresSql.Column(et, storeId, nameof(InboxMessage.ProcessedAtUtc));
            var lockedUntilCol = EfPostgresSql.Column(et, storeId, nameof(InboxMessage.LockedUntilUtc));
            var lockedOwnerCol = EfPostgresSql.Column(et, storeId, nameof(InboxMessage.LockedOwner));
            var attemptCol = EfPostgresSql.Column(et, storeId, nameof(InboxMessage.AttemptCount));
            var lastErrorCol = EfPostgresSql.Column(et, storeId, nameof(InboxMessage.LastError));
            var lastFailedAtCol = EfPostgresSql.Column(et, storeId, nameof(InboxMessage.LastFailedAtUtc));

            var sql = $@"
                UPDATE {table}
                SET {lastErrorCol} = {{0}},
                    {lastFailedAtCol} = {{1}},
                    {attemptCol} = {attemptCol} + 1,
                    {lockedUntilCol} = NULL,
                    {lockedOwnerCol} = NULL
                WHERE {eventIdCol} = {{2}}
                  AND {handlerCol} = {{3}}
                  AND {lockedOwnerCol} = {{4}}
                  AND {processedAtCol} IS NULL;";

            await db.Database.ExecuteSqlRawAsync(
                sql,
                [error, failedAtUtc, integrationEventId, handlerName, lockOwner],
                ct);
        }
    }
}
