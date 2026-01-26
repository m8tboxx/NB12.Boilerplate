using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NB12.Boilerplate.BuildingBlocks.Api.ResultHandling;
using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Application.Querying;
using NB12.Boilerplate.Host.Shared.Ops.Dtos;
using NB12.Boilerplate.Modules.Audit.Application.Queries.GetAuditRetentionConfig;
using NB12.Boilerplate.Modules.Audit.Application.Queries.GetAuditRetentionStatus;
using NB12.Boilerplate.Modules.Audit.Application.Queries.GetInboxStats;
using NB12.Boilerplate.Modules.Audit.Infrastructure.Persistence;
using NB12.Boilerplate.Modules.Auth.Application.Enums;
using NB12.Boilerplate.Modules.Auth.Application.Queries.GetOutboxMessages;
using NB12.Boilerplate.Modules.Auth.Application.Queries.GetOutboxStats;
using NB12.Boilerplate.Modules.Auth.Application.Security;
using NB12.Boilerplate.Modules.Auth.Infrastructure.Persistence;
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
                .WithTags("Ops");

            ops.MapGet("/overview", Overview)
                .RequireAuthorization(AuthPermissions.Auth.OpsRead);

            ops.MapGet("/outbox/stats", OutboxStats)
                .RequireAuthorization(AuthPermissions.Auth.OpsRead);

            ops.MapGet("/outbox/paged", OutboxPaged)
                .RequireAuthorization(AuthPermissions.Auth.OpsRead);

            ops.MapGet("/inbox/stats", InboxStats)
                .RequireAuthorization(AuthPermissions.Auth.OpsRead);

            ops.MapGet("/retention/config", RetentionConfig)
                .RequireAuthorization(AuthPermissions.Auth.OpsRead);

            ops.MapGet("/retention/status", RetentionStatus)
                .RequireAuthorization(AuthPermissions.Auth.OpsRead);

            ops.MapGet("/health/live", Live)
                .AllowAnonymous();

            ops.MapGet("/health/ready", Ready)
                .AllowAnonymous();
        }

        private static async Task<IResult> Overview(
            ISender sender,
            HttpContext http,
            IOptions<JsonOptions> jsonOptions,
            CancellationToken ct)
        {
            var outboxTask = sender.Send(new GetOutboxStatsQuery(), ct);
            var inboxTask = sender.Send(new GetInboxStatsQuery(), ct);
            var retentionConfigTask = sender.Send(new GetAuditRetentionConfigQuery(), ct);
            var retentionStatusTask = sender.Send(new GetAuditRetentionStatusQuery(), ct);

            await Task.WhenAll(outboxTask, inboxTask, retentionConfigTask, retentionStatusTask);

            if (!outboxTask.Result.IsSuccess)
                return outboxTask.Result.ToHttpResult(http);

            if (!inboxTask.Result.IsSuccess)
                return inboxTask.Result.ToHttpResult(http);

            if (!retentionConfigTask.Result.IsSuccess)
                return retentionConfigTask.Result.ToHttpResult(http);

            if (!retentionStatusTask.Result.IsSuccess)
                return retentionStatusTask.Result.ToHttpResult(http);

            // Meta (kann sich pro Request ändern)
            var meta = new OpsMetaDto(
                UtcNow: DateTime.UtcNow,
                CorrelationId: TryGetCorrelationId(http),
                TraceId: Activity.Current?.TraceId.ToString());

            // Data (soll ETag-stabil sein)
            var data = new OpsOverviewDataDto(
                Outbox: MapOutboxStats(outboxTask.Result.Value),
                Inbox: MapInboxStats(inboxTask.Result.Value),
                Retention: new OpsRetentionDto(
                    Config: MapRetentionConfig(retentionConfigTask.Result.Value),
                    Status: MapRetentionStatus(retentionStatusTask.Result.Value)));

            // ETag basiert NUR auf Data (nicht auf CorrelationId/TraceId)
            var etag = ComputeStrongETag(data, jsonOptions.Value.SerializerOptions);
            http.Response.Headers.ETag = etag;

            if (IfNoneMatchMatches(http.Request, etag))
                return Results.StatusCode(StatusCodes.Status304NotModified);

            var response = new OpsOverviewResponseDto(meta, data);
            return Results.Ok(response);
        }

        private static async Task<IResult> OutboxStats(ISender sender, HttpContext http, CancellationToken ct)
        {
            var res = await sender.Send(new GetOutboxStatsQuery(), ct);
            return res.ToHttpResult(http, x => Results.Ok(MapOutboxStats(x)));
        }

        private static async Task<IResult> InboxStats(ISender sender, HttpContext http, CancellationToken ct)
        {
            var res = await sender.Send(new GetInboxStatsQuery(), ct);
            return res.ToHttpResult(http, x => Results.Ok(MapInboxStats(x)));
        }

        private static async Task<IResult> RetentionConfig(ISender sender, HttpContext http, CancellationToken ct)
        {
            var res = await sender.Send(new GetAuditRetentionConfigQuery(), ct);
            return res.ToHttpResult(http, x => Results.Ok(MapRetentionConfig(x)));
        }

        private static async Task<IResult> RetentionStatus(ISender sender, HttpContext http, CancellationToken ct)
        {
            var res = await sender.Send(new GetAuditRetentionStatusQuery(), ct);
            return res.ToHttpResult(http, x => Results.Ok(MapRetentionStatus(x)));
        }

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
            ISender sender,
            HttpContext http,
            CancellationToken ct)
        {
            var parsedState = ParseState(state);
            var pr = new PageRequest(page <= 0 ? 1 : page, pageSize <= 0 ? 50 : pageSize);
            var sort = new Sort(sortBy, desc ? SortDirection.Desc : SortDirection.Asc);

            if (!includePayload)
            {
                var res = await sender.Send(new GetOutboxMessagesPagedQuery(
                    FromUtc: fromUtc,
                    ToUtc: toUtc,
                    Type: type,
                    State: parsedState,
                    Page: pr,
                    Sort: sort
                ), ct);

                return res.ToHttpResult(http, x =>
                {
                    var mapped = x.Items
                        .Select(m => new OpsOutboxMessageDto(
                            Id: m.Id,
                            OccurredAtUtc: m.OccurredAtUtc,
                            Type: m.Type,
                            AttemptCount: m.AttemptCount,
                            ProcessedAtUtc: m.ProcessedAtUtc,
                            LastError: m.LastError,
                            Content: null))
                        .ToList();

                    var dto = OpsPagedResponse<OpsOutboxMessageDto>.From(mapped, x.Page, x.PageSize, x.Total);
                    return Results.Ok(dto);
                });
            }

            var resWith = await sender.Send(new GetOutboxMessagesPagedWithDetailsQuery(
                FromUtc: fromUtc,
                ToUtc: toUtc,
                Type: type,
                State: parsedState,
                Page: pr,
                Sort: sort
            ), ct);

            return resWith.ToHttpResult(http, x =>
            {
                var mapped = x.Items
                    .Select(m => new OpsOutboxMessageDto(
                        Id: m.Id,
                        OccurredAtUtc: m.OccurredAtUtc,
                        Type: m.Type,
                        AttemptCount: m.AttemptCount,
                        ProcessedAtUtc: m.ProcessedAtUtc,
                        LastError: m.LastError,
                        Content: m.Content))
                    .ToList();

                var dto = OpsPagedResponse<OpsOutboxMessageDto>.From(mapped, x.Page, x.PageSize, x.Total);
                return Results.Ok(dto);
            });
        }

        private static IResult Live()
            => Results.Ok(new OpsHealthResponseDto(
                Status: "live",
                UtcNow: DateTime.UtcNow,
                Checks: Array.Empty<OpsHealthCheckDto>()));

        private static async Task<IResult> Ready(HttpContext http, CancellationToken ct)
        {
            var sp = http.RequestServices;

            var checks = new List<OpsHealthCheckDto>
            {
                await CheckDb<AuthDbContext>(sp, "AuthDb", ct),
                await CheckDb<AuditDbContext>(sp, "AuditDb", ct)
            };

            var ok = checks.All(c => c.Status == "ok");

            var response = new OpsHealthResponseDto(
                Status: ok ? "ready" : "degraded",
                UtcNow: DateTime.UtcNow,
                Checks: checks);

            return Results.Ok(response);
        }

        private static async Task<OpsHealthCheckDto> CheckDb<TDbContext>(IServiceProvider sp, string name, CancellationToken ct)
            where TDbContext : DbContext
        {
            try
            {
                var db = sp.GetService<TDbContext>();
                if (db is null)
                    return new OpsHealthCheckDto(name, "missing", "DbContext not registered in this host.");

                var canConnect = await db.Database.CanConnectAsync(ct);
                return canConnect
                    ? new OpsHealthCheckDto(name, "ok", null)
                    : new OpsHealthCheckDto(name, "fail", "Database connectivity check failed.");
            }
            catch (Exception ex)
            {
                return new OpsHealthCheckDto(name, "fail", ex.Message);
            }
        }

        private static OpsOutboxStatsDto MapOutboxStats(NB12.Boilerplate.Modules.Auth.Application.Responses.OutboxStatsDto s)
            => new(
                Total: s.Total,
                Pending: s.Pending,
                Failed: s.Failed,
                Processed: s.Processed,
                Locked: s.Locked,
                OldestPendingOccurredAtUtc: s.OldestPendingOccurredAtUtc,
                OldestFailedOccurredAtUtc: s.OldestFailedOccurredAtUtc);

        private static OpsInboxStatsDto MapInboxStats(NB12.Boilerplate.Modules.Audit.Application.Responses.InboxStatsDto s)
            => new(
                Total: s.Total,
                Pending: s.Pending,
                Failed: s.Failed,
                Processed: s.Processed,
                Locked: s.Locked,
                OldestPendingReceivedAtUtc: s.OldestPendingReceivedAtUtc,
                OldestFailedReceivedAtUtc: s.OldestFailedReceivedAtUtc);

        private static OpsRetentionConfigDto MapRetentionConfig(NB12.Boilerplate.Modules.Audit.Application.Responses.AuditRetentionConfigDto c)
            => new(
                Enabled: c.Enabled,
                RunEveryMinutes: c.RunEveryMinutes,
                RetainAuditLogsDays: c.RetainAuditLogsDays,
                RetainErrorLogsDays: c.RetainErrorLogsDays);

        private static OpsRetentionStatusDto MapRetentionStatus(NB12.Boilerplate.Modules.Audit.Application.Responses.AuditRetentionStatusDto s)
            => new(
                Enabled: s.Enabled,
                LastRunAtUtc: s.LastRunAtUtc,
                LastDeletedAuditLogs: s.LastDeletedAuditLogs,
                LastDeletedErrorLogs: s.LastDeletedErrorLogs,
                LastError: s.LastError);

        private static OutboxMessageState ParseState(string? state)
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

            // Strong ETag requires quotes
            return "\"" + Convert.ToHexString(hash).ToLowerInvariant() + "\"";
        }

        private static bool IfNoneMatchMatches(HttpRequest request, string etag)
        {
            if (!request.Headers.TryGetValue("If-None-Match", out var values))
                return false;

            var raw = values.ToString();
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            // Can be "*" or list: W/"abc", "def"
            if (raw.Trim() == "*")
                return true;

            var parts = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var p in parts)
            {
                // accept weak match too: W/"..."
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
