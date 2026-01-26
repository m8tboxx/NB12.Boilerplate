using Microsoft.EntityFrameworkCore;
using NB12.Boilerplate.BuildingBlocks.Application.Querying;
using NB12.Boilerplate.Modules.Audit.Application.Enums;
using NB12.Boilerplate.Modules.Audit.Application.Interfaces;
using NB12.Boilerplate.Modules.Audit.Application.Responses;
using NB12.Boilerplate.Modules.Audit.Infrastructure.Persistence;
using Npgsql;
using System.Text;

namespace NB12.Boilerplate.Modules.Audit.Infrastructure.Repositories
{
    internal sealed class InboxAdminRepository : IInboxAdminRepository
    {
        private readonly AuditDbContext _db;

        public InboxAdminRepository(AuditDbContext db) => _db = db;

        public async Task<PagedResponse<InboxMessageDto>> GetPagedAsync(
            Guid? integrationEventId,
            string? handlerName,
            InboxMessageState state,
            DateTime? fromUtc,
            DateTime? toUtc,
            PageRequest page,
            Sort sort,
            CancellationToken ct)
        {
            var (whereSql, parameters) = BuildWhere(integrationEventId, handlerName, state, fromUtc, toUtc);

            var totalSql = $"SELECT COUNT(*)::bigint AS \"Value\" FROM \"audit\".\"InboxMessages\" {whereSql}" ;
            var total = await _db.Database.SqlQueryRaw<long>(totalSql, parameters.ToArray()).SingleAsync(ct);

            var (orderBySql, dirSql) = BuildOrderBy(sort);
            var sql = new StringBuilder();
            sql.Append("SELECT ");
            sql.Append("\"IntegrationEventId\", \"HandlerName\", \"ReceivedAtUtc\", \"ProcessedAtUtc\", ");
            sql.Append("\"AttemptCount\", \"LastFailedAtUtc\", \"LastError\", \"LockedUntilUtc\", \"LockedOwner\" ");
            sql.Append("FROM \"audit\".\"InboxMessages\" ");
            sql.Append(whereSql);
            sql.Append($" ORDER BY {orderBySql} {dirSql} ");
            sql.Append("OFFSET @skip LIMIT @take;");

            parameters.Add(new NpgsqlParameter("skip", page.Skip));
            parameters.Add(new NpgsqlParameter("take", page.PageSize));

            var rows = await _db.Database.SqlQueryRaw<InboxRow>(sql.ToString(), parameters.ToArray())
                .ToListAsync(ct);

            var items = rows.Select(r => new InboxMessageDto(
                r.IntegrationEventId,
                r.HandlerName,
                r.ReceivedAtUtc,
                r.ProcessedAtUtc,
                r.AttemptCount,
                r.LastFailedAtUtc,
                r.LastError,
                r.LockedUntilUtc,
                r.LockedOwner
            )).ToList();

            return new PagedResponse<InboxMessageDto>(items, page.Page, page.PageSize, total);
        }

        public async Task<InboxStatsDto> GetStatsAsync(CancellationToken ct)
        {
            var total = await ScalarAsync<long>(
                "SELECT COUNT(*)::bigint AS \"Value\" FROM \"audit\".\"InboxMessages\";", ct);

            var pending = await ScalarAsync<long>(
                "SELECT COUNT(*)::bigint AS \"Value\" FROM \"audit\".\"InboxMessages\" WHERE \"ProcessedAtUtc\" IS NULL;",
                ct);

            var failed = await ScalarAsync<long>(
                "SELECT COUNT(*)::bigint AS \"Value\" FROM \"audit\".\"InboxMessages\" WHERE \"ProcessedAtUtc\" IS NULL AND \"AttemptCount\" > 0;",
                ct);

            var processed = await ScalarAsync<long>(
                "SELECT COUNT(*)::bigint AS \"Value\" FROM \"audit\".\"InboxMessages\" WHERE \"ProcessedAtUtc\" IS NOT NULL;",
                ct);

            var locked = await ScalarAsync<long>(
                "SELECT COUNT(*)::bigint AS \"Value\" FROM \"audit\".\"InboxMessages\" WHERE \"LockedUntilUtc\" IS NOT NULL AND \"LockedUntilUtc\" > NOW() AT TIME ZONE 'UTC';",
                ct);

            var oldestPending = await ScalarAsync<DateTime?>(
                "SELECT MIN(\"ReceivedAtUtc\") AS \"Value\" FROM \"audit\".\"InboxMessages\" WHERE \"ProcessedAtUtc\" IS NULL;",
                ct);

            var oldestFailed = await ScalarAsync<DateTime?>(
                "SELECT MIN(\"ReceivedAtUtc\") AS \"Value\" FROM \"audit\".\"InboxMessages\" WHERE \"ProcessedAtUtc\" IS NULL AND \"AttemptCount\" > 0;",
                ct);

            return new InboxStatsDto(total, pending, failed, processed, locked, oldestPending, oldestFailed);
        }

        public async Task<bool> DeleteAsync(Guid integrationEventId, string handlerName, CancellationToken ct)
        {
            var sql = "DELETE FROM \"audit\".\"InboxMessages\" WHERE \"IntegrationEventId\" = @id AND \"HandlerName\" = @handler;";
            var affected = await _db.Database.ExecuteSqlRawAsync(sql,
                new NpgsqlParameter("id", integrationEventId),
                new NpgsqlParameter("handler", handlerName));

            return affected > 0;
        }

        public async Task<int> CleanupProcessedBeforeAsync(DateTime beforeUtc, int maxRows, CancellationToken ct)
        {
            // Postgres-safe limited delete via ctid
            var sql = @"
                DELETE FROM ""audit"".""InboxMessages""
                WHERE ctid IN (
                    SELECT ctid
                    FROM ""audit"".""InboxMessages""
                    WHERE ""ProcessedAtUtc"" IS NOT NULL
                      AND ""ProcessedAtUtc"" < @before
                    ORDER BY ""ProcessedAtUtc""
                    LIMIT @limit
                );";

            return await _db.Database.ExecuteSqlRawAsync(sql,
                new NpgsqlParameter("before", beforeUtc),
                new NpgsqlParameter("limit", maxRows));
        }

        private static (string WhereSql, List<NpgsqlParameter> Parameters) BuildWhere(
            Guid? integrationEventId,
            string? handlerName,
            InboxMessageState state,
            DateTime? fromUtc,
            DateTime? toUtc)
        {
            var parts = new List<string>();
            var ps = new List<NpgsqlParameter>();

            if (integrationEventId is not null)
            {
                parts.Add("\"IntegrationEventId\" = @eventId");
                ps.Add(new NpgsqlParameter("eventId", integrationEventId.Value));
            }

            if (!string.IsNullOrWhiteSpace(handlerName))
            {
                parts.Add("\"HandlerName\" ILIKE @handlerName");
                ps.Add(new NpgsqlParameter("handlerName", $"%{handlerName}%"));
            }

            if (fromUtc is not null)
            {
                parts.Add("\"ReceivedAtUtc\" >= @fromUtc");
                ps.Add(new NpgsqlParameter("fromUtc", fromUtc.Value));
            }

            if (toUtc is not null)
            {
                parts.Add("\"ReceivedAtUtc\" <= @toUtc");
                ps.Add(new NpgsqlParameter("toUtc", toUtc.Value));
            }

            parts.Add(state switch
            {
                InboxMessageState.Pending => "\"ProcessedAtUtc\" IS NULL AND \"AttemptCount\" = 0",
                InboxMessageState.Failed => "\"ProcessedAtUtc\" IS NULL AND \"AttemptCount\" > 0",
                InboxMessageState.Processed => "\"ProcessedAtUtc\" IS NOT NULL",
                InboxMessageState.Locked => "\"LockedUntilUtc\" IS NOT NULL AND \"LockedUntilUtc\" > NOW() AT TIME ZONE 'UTC'",
                _ => "TRUE"
            });

            var where = parts.Count == 0 ? "" : "WHERE " + string.Join(" AND ", parts);
            return (where, ps);
        }

        private static (string OrderBySql, string DirSql) BuildOrderBy(Sort sort)
        {
            var by = (sort.By ?? "receivedAtUtc").Trim().ToLowerInvariant();
            var dir = sort.Direction == SortDirection.Asc ? "ASC" : "DESC";

            var orderBy = by switch
            {
                "attemptcount" => "\"AttemptCount\"",
                "processedatutc" => "\"ProcessedAtUtc\"",
                "handlername" => "\"HandlerName\"",
                "lockeduntilutc" => "\"LockedUntilUtc\"",
                _ => "\"ReceivedAtUtc\""
            };

            return (orderBy, dir);
        }

        private async Task<T> ScalarAsync<T>(string sql, CancellationToken ct)
        {
            sql = sql.Trim();
            sql = sql.TrimEnd(';');
            return await _db.Database.SqlQueryRaw<T>(sql).SingleAsync(ct);
        }

        private sealed class InboxRow
        {
            public Guid IntegrationEventId { get; set; }
            public string HandlerName { get; set; } = string.Empty;
            public DateTime ReceivedAtUtc { get; set; }
            public DateTime? ProcessedAtUtc { get; set; }
            public int AttemptCount { get; set; }
            public DateTime? LastFailedAtUtc { get; set; }
            public string? LastError { get; set; }
            public DateTime? LockedUntilUtc { get; set; }
            public string? LockedOwner { get; set; }
        }
    }
}
