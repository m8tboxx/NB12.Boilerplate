using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NB12.Boilerplate.BuildingBlocks.Api.Modularity;
using NB12.Boilerplate.BuildingBlocks.Application.Enums;
using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration.Admin;
using NB12.Boilerplate.BuildingBlocks.Application.Querying;
using NB12.Boilerplate.Host.Shared.Ops.Dtos;
using NB12.Boilerplate.Modules.Audit.Application.Interfaces;
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

            ops.MapGet("/outbox/modules", OutboxModules);
            ops.MapGet("/inbox/modules", InboxModules);

            // OUTBOX
            ops.MapGet("/outbox/{moduleKey}/stats", OutboxStats);
            ops.MapGet("/outbox/{moduleKey}/paged", OutboxPaged);
            ops.MapPost("/outbox/{moduleKey}/{id:guid}/replay", OutboxReplay).RequireAuthorization(AuthPermissions.Auth.OpsWrite);
            ops.MapDelete("/outbox/{moduleKey}/{id:guid}", OutboxDelete).RequireAuthorization(AuthPermissions.Auth.OpsWrite);

            // INBOX
            ops.MapGet("/inbox/{moduleKey}/stats", InboxStats);
            ops.MapGet("/inbox/{moduleKey}/paged", InboxPaged);
            ops.MapPost("/inbox/{moduleKey}/{id:guid}/replay", InboxReplay).RequireAuthorization(AuthPermissions.Auth.OpsWrite);
            ops.MapDelete("/inbox/{moduleKey}/{id:guid}", InboxDelete).RequireAuthorization(AuthPermissions.Auth.OpsWrite);

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
            [FromServices] IEnumerable<IModuleKeyProvider> modules,
            [FromServices] IOutboxAdminStoreResolver outboxResolver,
            [FromServices] IInboxAdminStoreResolver inboxResolver,
            [FromServices] IAuditRetentionStatusProvider retentionStatus,
            [FromServices] IAuditRetentionConfigProvider retentionConfig,
            [FromServices] JsonSerializerOptions json,
            CancellationToken ct)
        {
            var keys = modules
                .Select(m => m.ModuleKey)
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            // Stats: aggregate across all modules that actually have stores registered
            var outboxTasks = new List<Task<OutboxAdminStatsDto>>(keys.Length);
            var inboxTasks = new List<Task<InboxAdminStatsDto>>(keys.Length);

            foreach (var k in keys)
            {
                if (outboxResolver.TryGet(k, out var outbox))
                    outboxTasks.Add(outbox.GetStatsAsync(ct));

                if (inboxResolver.TryGet(k, out var inbox))
                    inboxTasks.Add(inbox.GetStatsAsync(ct));
            }

            var cfgTask = retentionConfig.GetAsync(ct);
            var statusTask = retentionStatus.GetAsync(ct);

            await Task.WhenAll(outboxTasks.Concat<Task>(inboxTasks).Append(cfgTask).Append(statusTask));

            var outboxAgg = AggregateOutbox(await Task.WhenAll(outboxTasks));
            var inboxAgg = AggregateInbox(await Task.WhenAll(inboxTasks));

            var cfg = await cfgTask;
            var status = await statusTask;

            var meta = new OpsMetaDto(
                UtcNow: DateTime.UtcNow,
                CorrelationId: TryGetCorrelationId(http),
                TraceId: Activity.Current?.TraceId.ToString());

            var data = new OpsOverviewDataDto(
                Outbox: new OpsOutboxStatsDto(
                    Total: outboxAgg.Total,
                    Pending: outboxAgg.Pending,
                    Failed: outboxAgg.Failed,
                    Processed: outboxAgg.Processed,
                    Locked: outboxAgg.Locked,
                    OldestPendingOccurredAtUtc: outboxAgg.OldestPendingOccurredAtUtc,
                    OldestFailedOccurredAtUtc: outboxAgg.OldestFailedOccurredAtUtc),
                Inbox: new OpsInboxStatsDto(
                    Total: inboxAgg.Total,
                    Pending: inboxAgg.Pending,
                    Failed: inboxAgg.Failed,
                    Processed: inboxAgg.Processed,
                    Locked: inboxAgg.Locked,
                    OldestPendingReceivedAtUtc: inboxAgg.OldestPendingReceivedAtUtc,
                    OldestFailedReceivedAtUtc: inboxAgg.OldestFailedReceivedAtUtc),
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


        private static IResult OutboxModules(
            [FromServices] IEnumerable<IModuleKeyProvider> modules,
            [FromServices] IOutboxAdminStoreResolver resolver)
        {
            var keys = modules
                .Select(m => m.ModuleKey)
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(k => resolver.TryGet(k, out _))
                .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return Results.Ok(keys);
        }

        private static IResult InboxModules(
            [FromServices] IEnumerable<IModuleKeyProvider> modules,
            [FromServices] IInboxAdminStoreResolver resolver)
        {
            var keys = modules
                .Select(m => m.ModuleKey)
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(k => resolver.TryGet(k, out _))
                .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return Results.Ok(keys);
        }


        private static async Task<IResult> OutboxStats(
            string moduleKey,
            [FromServices] IOutboxAdminStoreResolver resolver,
            CancellationToken ct)
        {
            if (!resolver.TryGet(moduleKey, out var store))
                return Results.NotFound($"Unknown moduleKey '{moduleKey}'");

            var s = await store.GetStatsAsync(ct);
            return Results.Ok(new OpsOutboxStatsDto(
                s.Total, s.Pending, s.Failed, s.Processed, s.Locked,
                s.OldestPendingOccurredAtUtc,
                s.OldestFailedOccurredAtUtc));
        }


        private static async Task<IResult> InboxStats(
            string moduleKey,
            [FromServices] IInboxAdminStoreResolver resolver,
            CancellationToken ct)
        {
            if (!resolver.TryGet(moduleKey, out var store))
                return Results.NotFound($"Unknown moduleKey '{moduleKey}'");

            var s = await store.GetStatsAsync(ct);
            return Results.Ok(new OpsInboxStatsDto(
                s.Total, s.Pending, s.Failed, s.Processed, s.Locked,
                s.OldestPendingReceivedAtUtc,
                s.OldestFailedReceivedAtUtc));
        }


        private static async Task<IResult> OutboxPaged(
            string moduleKey,
            DateTime? fromUtc,
            DateTime? toUtc,
            string? type,
            string? state,
            string? sortBy,
            int page,
            int pageSize,
            bool includePayload,
            bool desc,
            [FromServices] IOutboxAdminStoreResolver resolver,
            CancellationToken ct)
        {
            if (!resolver.TryGet(moduleKey, out var store))
                return Results.NotFound($"Unknown moduleKey '{moduleKey}'");

            var parsedState = ParseOutboxState(state);
            var pr = new PageRequest(page <= 0 ? 1 : page, pageSize <= 0 ? 50 : pageSize);
            var sort = new Sort(sortBy, desc ? SortDirection.Desc : SortDirection.Asc);

            if (!includePayload)
            {
                var res = await store.GetPagedAsync(parsedState, type, fromUtc, toUtc, pr, sort, ct);
                var mapped = res.Items.Select(x => new OpsOutboxMessageDto(
                    x.Id, x.OccurredAtUtc, x.Type, x.AttemptCount, x.ProcessedAtUtc, x.LastError, null));

                return Results.Ok(OpsPagedResponse<OpsOutboxMessageDto>.From(mapped, res.Page, res.PageSize, res.Total));
            }
            else
            {
                var res = await store.GetPagedWithDetailsAsync(parsedState, type, fromUtc, toUtc, pr, sort, ct);
                var mapped = res.Items.Select(x => new OpsOutboxMessageDto(
                    x.Id, x.OccurredAtUtc, x.Type, x.AttemptCount, x.ProcessedAtUtc, x.LastError, x.Content));

                return Results.Ok(OpsPagedResponse<OpsOutboxMessageDto>.From(mapped, res.Page, res.PageSize, res.Total));
            }
        }


        private static async Task<IResult> InboxPaged(
            string moduleKey,
            Guid? integrationEventId,
            string? handlerName,
            string? state,
            DateTime? fromUtc,
            DateTime? toUtc,
            int page,
            int pageSize,
            string? sortBy,
            bool desc,
            bool includePayload,
            [FromServices] IInboxAdminStoreResolver resolver,
            CancellationToken ct)
        {
            if (!resolver.TryGet(moduleKey, out var store))
                return Results.NotFound($"Unknown moduleKey '{moduleKey}'");

            var parsed = ParseInboxState(state);
            var pr = new PageRequest(page <= 0 ? 1 : page, pageSize <= 0 ? 50 : pageSize);
            var sort = new Sort(sortBy, desc ? SortDirection.Desc : SortDirection.Asc);

            var res = await store.GetPagedAsync(integrationEventId, handlerName, parsed, fromUtc, toUtc, pr, sort, ct);

            if (!includePayload)
            {
                var mapped = res.Items.Select(x => new OpsInboxMessageDto(
                    x.Id.Value, x.IntegrationEventId, x.HandlerName, x.EventType,
                    x.ReceivedAtUtc, x.ProcessedAtUtc, x.AttemptCount, x.LastError, null));

                return Results.Ok(OpsPagedResponse<OpsInboxMessageDto>.From(mapped, res.Page, res.PageSize, res.Total));
            }

            // includePayload: map per message id with details call (performance ok für Ops)
            var details = new List<OpsInboxMessageDto>(res.Items.Count);
            foreach (var m in res.Items)
            {
                var d = await store.GetByIdAsync(m.Id, ct);
                details.Add(new OpsInboxMessageDto(
                    m.Id.Value, m.IntegrationEventId, m.HandlerName, m.EventType,
                    m.ReceivedAtUtc, m.ProcessedAtUtc, m.AttemptCount, m.LastError,
                    d?.PayloadJson));
            }

            return Results.Ok(OpsPagedResponse<OpsInboxMessageDto>.From(details, res.Page, res.PageSize, res.Total));
        }


        private static async Task<IResult> OutboxReplay(
            string moduleKey,
            Guid id,
            [FromServices] IOutboxAdminStoreResolver resolver,
            CancellationToken ct)
        {
            if (!resolver.TryGet(moduleKey, out var store))
                return Results.NotFound($"Unknown moduleKey '{moduleKey}'");

            var result = await store.ReplayAsync(id, ct);
            return result switch
            {
                OutboxAdminWriteResult.Ok => Results.NoContent(),
                OutboxAdminWriteResult.NotFound => Results.NotFound(),
                OutboxAdminWriteResult.Locked => Results.BadRequest(),
                _ => Results.Problem($"Unexpected result OutboxReplay: {result}")
            };
        }


        private static async Task<IResult> OutboxDelete(
            string moduleKey,
            Guid id,
            [FromServices] IOutboxAdminStoreResolver resolver,
            CancellationToken ct)
        {
            if (!resolver.TryGet(moduleKey, out var store))
                return Results.NotFound($"Unknown moduleKey '{moduleKey}'");

            var result = await store.DeleteAsync(id, ct);
            return result switch
            {
                OutboxAdminWriteResult.Ok => Results.NoContent(),
                OutboxAdminWriteResult.NotFound => Results.NotFound(),
                OutboxAdminWriteResult.Locked => Results.BadRequest(),
                _ => Results.Problem($"Unexpected result OutboxDelete: {result}")
            };
        }


        private static async Task<IResult> InboxReplay(
            string moduleKey,
            Guid id,
            [FromServices] IInboxAdminStoreResolver resolver,
            CancellationToken ct)
        {
            if (!resolver.TryGet(moduleKey, out var store))
                return Results.NotFound($"Unknown moduleKey '{moduleKey}'");

            var result = await store.ReplayAsync(new BuildingBlocks.Application.Ids.InboxMessageId(id), ct);

            return result switch
            {
                InboxAdminWriteResult.Ok => Results.NoContent(),
                InboxAdminWriteResult.NotFound => Results.NotFound(),
                InboxAdminWriteResult.Locked => Results.BadRequest(),
                _ => Results.Problem($"Unexpected result InboxReplay: {result}")
            };
        }


        private static async Task<IResult> InboxDelete(
            string moduleKey,
            Guid id,
            [FromServices] IInboxAdminStoreResolver resolver,
            CancellationToken ct)
        {
            if (!resolver.TryGet(moduleKey, out var store))
                return Results.NotFound($"Unknown moduleKey '{moduleKey}'");

            var result = await store.DeleteAsync(new BuildingBlocks.Application.Ids.InboxMessageId(id), ct);

            return result switch
            {
                InboxAdminWriteResult.Ok => Results.NoContent(),
                InboxAdminWriteResult.NotFound => Results.NotFound(),
                InboxAdminWriteResult.Locked => Results.BadRequest(),
                _ => Results.Problem($"Unexpected result InboxDelete: {result}")
            };
        }


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
                return InboxMessageState.All;

            return state.Trim().ToLowerInvariant() switch
            {
                "pending" => InboxMessageState.Pending,
                "failed" => InboxMessageState.Failed,
                "processed" => InboxMessageState.Processed,
                "deadlettered" => InboxMessageState.DeadLettered,
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

        private static OutboxAdminStatsDto AggregateOutbox(OutboxAdminStatsDto[] stats)
        {
            long total = 0, pending = 0, failed = 0, processed = 0, locked = 0;
            DateTime? oldestPending = null;
            DateTime? oldestFailed = null;

            foreach (var s in stats)
            {
                total += s.Total;
                pending += s.Pending;
                failed += s.Failed;
                processed += s.Processed;
                locked += s.Locked;

                if (s.OldestPendingOccurredAtUtc is not null)
                    oldestPending = oldestPending is null ? s.OldestPendingOccurredAtUtc : (s.OldestPendingOccurredAtUtc < oldestPending ? s.OldestPendingOccurredAtUtc : oldestPending);

                if (s.OldestFailedOccurredAtUtc is not null)
                    oldestFailed = oldestFailed is null ? s.OldestFailedOccurredAtUtc : (s.OldestFailedOccurredAtUtc < oldestFailed ? s.OldestFailedOccurredAtUtc : oldestFailed);
            }

            return new OutboxAdminStatsDto(total, pending, failed, processed, locked, oldestPending, oldestFailed);
        }

        private static InboxAdminStatsDto AggregateInbox(InboxAdminStatsDto[] stats)
        {
            long total = 0, pending = 0, failed = 0, processed = 0, locked = 0;
            DateTime? oldestPending = null;
            DateTime? oldestFailed = null;

            foreach (var s in stats)
            {
                total += s.Total;
                pending += s.Pending;
                failed += s.Failed;
                processed += s.Processed;
                locked += s.Locked;

                if (s.OldestPendingReceivedAtUtc is not null)
                    oldestPending = oldestPending is null ? s.OldestPendingReceivedAtUtc : (s.OldestPendingReceivedAtUtc < oldestPending ? s.OldestPendingReceivedAtUtc : oldestPending);

                if (s.OldestFailedReceivedAtUtc is not null)
                    oldestFailed = oldestFailed is null ? s.OldestFailedReceivedAtUtc : (s.OldestFailedReceivedAtUtc < oldestFailed ? s.OldestFailedReceivedAtUtc : oldestFailed);
            }

            return new InboxAdminStatsDto(total, pending, failed, processed, locked, oldestPending, oldestFailed);
        }
    }
}
