using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Application.Querying;
using NB12.Boilerplate.Host.Shared.Ops.Dtos;
using NB12.Boilerplate.Modules.Audit.Application.Enums;
using NB12.Boilerplate.Modules.Audit.Application.Interfaces;
using NB12.Boilerplate.Modules.Audit.Domain.Ids;
using NB12.Boilerplate.Modules.Auth.Application.Enums;
using NB12.Boilerplate.Modules.Auth.Application.Interfaces;
using NB12.Boilerplate.Modules.Auth.Application.Security;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace NB12.Boilerplate.Host.Shared.Ops
{
    public static class OpsEndpoints
    {
        public static void MapOpsEndpoints(IEndpointRouteBuilder app)
        {
            var ops = app.MapGroup("/api/ops")
                .WithTags("Ops")
                .RequireAuthorization(AuthPermissions.Auth.OpsRead);

            ops.MapGet("/overview", Overview);

            // OUTBOX (Auth)
            ops.MapGet("/outbox/stats", OutboxStats);
            ops.MapGet("/outbox/paged", OutboxPaged);
            ops.MapPost("/outbox/{id:guid}/replay", OutboxReplay).RequireAuthorization(AuthPermissions.Auth.OpsWrite);
            ops.MapDelete("/outbox/{id:guid}", OutboxDelete).RequireAuthorization(AuthPermissions.Auth.OpsWrite);

            // INBOX (Audit)
            ops.MapGet("/inbox/stats", InboxStats);
            ops.MapGet("/inbox/paged", InboxPaged);
            ops.MapPost("/inbox/{id:guid}/replay", InboxReplay).RequireAuthorization(AuthPermissions.Auth.OpsWrite);
            ops.MapDelete("/inbox/{id:guid}", InboxDelete).RequireAuthorization(AuthPermissions.Auth.OpsWrite);

            // RETENTION (Audit)
            ops.MapGet("/retention/config", RetentionConfig);
            ops.MapGet("/retention/status", RetentionStatus);
            ops.MapPost("/retention/run", RetentionRunNow).RequireAuthorization(AuthPermissions.Auth.OpsWrite);

            // Health
            ops.MapGet("/health/live", Live).AllowAnonymous();
            ops.MapGet("/health/ready", Ready).AllowAnonymous();
        }

        private static async Task<IResult> Overview(
            HttpContext http,
            [FromServices] ISender sender,
            [FromServices] IOutboxAdminRepository outbox,
            [FromServices] IInboxAdminRepository inbox,
            [FromServices] IAuditRetentionStatusProvider retentionStatus,
            [FromServices] IAuditRetentionConfigProvider retentionConfig,
            [FromServices] JsonSerializerOptions json,
            CancellationToken ct)
        {
            // Parallel is safe: repositories/services use factories or no DbContext sharing.
            var outboxStatsTask = outbox.GetStatsAsync(ct);
            var inboxStatsTask = inbox.GetStatsAsync(ct);
            var cfgTask = retentionConfig.GetAsync(ct);
            var statusTask = retentionStatus.GetAsync(ct);

            await Task.WhenAll(outboxStatsTask, inboxStatsTask, cfgTask, statusTask);

            var outboxStats = await outboxStatsTask;
            var inboxStats = await inboxStatsTask;
            var cfg = await cfgTask;
            var status = await statusTask;

            var meta = new OpsMetaDto(
                UtcNow: DateTime.UtcNow,
                CorrelationId: TryGetCorrelationId(http),
                TraceId: Activity.Current?.TraceId.ToString());

            var data = new OpsOverviewDataDto(
                Outbox: new OpsOutboxStatsDto(
                    Total: outboxStats.Total,
                    Pending: outboxStats.Pending,
                    Failed: outboxStats.Failed,
                    Processed: outboxStats.Processed,
                    Locked: outboxStats.Locked,
                    OldestPendingOccurredAtUtc: outboxStats.OldestPendingOccurredAtUtc,
                    OldestFailedOccurredAtUtc: outboxStats.OldestFailedOccurredAtUtc),
                Inbox: new OpsInboxStatsDto(
                    Total: inboxStats.Total,
                    Pending: inboxStats.Pending,
                    Failed: inboxStats.Failed,
                    Processed: inboxStats.Processed,
                    Locked: inboxStats.Locked,
                    OldestPendingReceivedAtUtc: inboxStats.OldestPendingReceivedAtUtc,
                    OldestFailedReceivedAtUtc: inboxStats.OldestFailedReceivedAtUtc),
                Retention: new OpsRetentionDto(
                    Config: new OpsRetentionConfigDto(
                        Enabled: cfg.Enabled,
                        RunEveryMinutes: cfg.RunEveryMinutes,
                        RetainAuditLogsDays: cfg.RetainAuditLogsDays,
                        RetainErrorLogsDays: cfg.RetainErrorLogsDays),
                    Status: new OpsRetentionStatusDto(
                        Enabled: status.Enabled,
                        LastRunAtUtc: status.LastRunAtUtc,
                        LastDeletedAuditLogs: status.LastDeletedAuditLogs,
                        LastDeletedErrorLogs: status.LastDeletedErrorLogs,
                        LastError: status.LastError)));

            var etag = ComputeStrongETag(data, json);
            http.Response.Headers.ETag = etag;

            if (IfNoneMatchMatches(http.Request, etag))
                return Results.StatusCode(StatusCodes.Status304NotModified);

            return Results.Ok(new OpsOverviewResponseDto(meta, data));
        }

        private static async Task<IResult> OutboxStats([FromServices] IOutboxAdminRepository repo, CancellationToken ct)
            => Results.Ok(await repo.GetStatsAsync(ct));

        private static async Task<IResult> InboxStats([FromServices] IInboxAdminRepository repo, CancellationToken ct)
            => Results.Ok(await repo.GetStatsAsync(ct));

        private static async Task<IResult> OutboxPaged(
            DateTime? fromUtc,
            DateTime? toUtc,
            string? type,
            string? state,
            string? sortBy,
            int page,
            int pageSize,
            bool includePayload,
            bool desc,
            [FromServices] IOutboxAdminRepository repo,
            CancellationToken ct)
        {
            var parsedState = ParseOutboxState(state);
            var pr = new PageRequest(page <= 0 ? 1 : page, pageSize <= 0 ? 50 : pageSize);
            var sort = new Sort(sortBy, desc ? SortDirection.Desc : SortDirection.Asc);

            if (!includePayload)
                return Results.Ok(await repo.GetPagedAsync(fromUtc, toUtc, type, parsedState, pr, sort, ct));

            return Results.Ok(await repo.GetPagedWithDetailsAsync(fromUtc, toUtc, type, parsedState, pr, sort, ct));
        }

        private static async Task<IResult> InboxPaged(
            Guid? integrationEventId,
            string? handlerName,
            string? state,
            DateTime? fromUtc,
            DateTime? toUtc,
            int page,
            int pageSize,
            string? sortBy,
            bool desc,
            [FromServices] IInboxAdminRepository repo,
            CancellationToken ct)
        {
            var parsed = ParseInboxState(state);
            var pr = new PageRequest(page <= 0 ? 1 : page, pageSize <= 0 ? 50 : pageSize);
            var sort = new Sort(sortBy, desc ? SortDirection.Desc : SortDirection.Asc);

            return Results.Ok(await repo.GetPagedAsync(integrationEventId, handlerName, parsed, fromUtc, toUtc, pr, sort, ct));
        }

        private static async Task<IResult> OutboxReplay(Guid id, [FromServices] IOutboxAdminRepository repo, CancellationToken ct)
            => (await repo.ReplayAsync(id, ct)) ? Results.NoContent() : Results.NotFound();

        private static async Task<IResult> OutboxDelete(Guid id, [FromServices] IOutboxAdminRepository repo, CancellationToken ct)
            => (await repo.DeleteAsync(id, ct)) ? Results.NoContent() : Results.NotFound();

        private static async Task<IResult> InboxReplay(InboxMessageId id, [FromServices] IInboxAdminRepository repo, CancellationToken ct)
            => (await repo.ReplayAsync(id, ct)) ? Results.NoContent() : Results.NotFound();

        private static async Task<IResult> InboxDelete(InboxMessageId id, [FromServices] IInboxAdminRepository repo, CancellationToken ct)
            => (await repo.DeleteAsync(id, ct)) ? Results.NoContent() : Results.NotFound();

        private static async Task<IResult> RetentionConfig([FromServices] IAuditRetentionConfigProvider provider, CancellationToken ct)
            => Results.Ok(await provider.GetAsync(ct));

        private static async Task<IResult> RetentionStatus([FromServices] IAuditRetentionStatusProvider provider, CancellationToken ct)
            => Results.Ok(await provider.GetAsync(ct));

        private static async Task<IResult> RetentionRunNow([FromServices] IAuditRetentionService svc, CancellationToken ct)
        {
            await svc.RunOnceAsync(ct);
            return Results.Accepted();
        }

        private static IResult Live()
            => Results.Ok(new OpsHealthResponseDto(
                Status: "live",
                UtcNow: DateTime.UtcNow,
                Checks: Array.Empty<OpsHealthCheckDto>()));

        private static async Task<IResult> Ready(HttpContext http, CancellationToken ct)
        {
            // If your ready-check uses DbContexts, keep it lightweight or do it in dedicated endpoints.
            // For now: ready == live (you can extend later).
            await Task.CompletedTask;
            return Results.Ok(new OpsHealthResponseDto(
                Status: "ready",
                UtcNow: DateTime.UtcNow,
                Checks: Array.Empty<OpsHealthCheckDto>()));
        }

        private static OutboxMessageState ParseOutboxState(string? state)
        {
            if (string.IsNullOrWhiteSpace(state))
                return OutboxMessageState.All;

            return state.Trim().ToLowerInvariant() switch
            {
                "pending" => OutboxMessageState.Pending,
                "failed" => OutboxMessageState.Failed,
                "processed" => OutboxMessageState.Processed,
                "deadlettered" => OutboxMessageState.DeadLettered,
                _ => OutboxMessageState.All
            };
        }

        private static InboxMessageState ParseInboxState(string? state)
        {
            if (string.IsNullOrWhiteSpace(state))
                return NB12.Boilerplate.Modules.Audit.Application.Enums.InboxMessageState.All;

            return state.Trim().ToLowerInvariant() switch
            {
                "pending" => InboxMessageState.Pending,
                "failed" => InboxMessageState.Failed,
                "processed" => InboxMessageState.Processed,
                _ => InboxMessageState.All
            };
        }

        private static string? TryGetCorrelationId(HttpContext http)
        {
            if (http.Response.Headers.TryGetValue("X-Correlation-Id", out var cid))
                return cid.ToString();

            if (http.Request.Headers.TryGetValue("X-Correlation-Id", out var rcid))
                return rcid.ToString();

            return null;
        }

        private static string ComputeStrongETag<T>(T data, JsonSerializerOptions options)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(data, options);
            var hash = SHA256.HashData(bytes);
            return "\"" + Convert.ToHexString(hash).ToLowerInvariant() + "\"";
        }

        private static bool IfNoneMatchMatches(HttpRequest request, string etag)
        {
            if (!request.Headers.TryGetValue("If-None-Match", out var values))
                return false;

            var raw = values.ToString();
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            if (raw.Trim() == "*")
                return true;

            var parts = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var p in parts)
            {
                var token = p.StartsWith("W/", StringComparison.OrdinalIgnoreCase)
                    ? p.Substring(2).Trim()
                    : p.Trim();

                if (string.Equals(token, etag, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }
}
