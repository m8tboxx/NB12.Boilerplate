using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NB12.Boilerplate.BuildingBlocks.Api.ResultHandling;
using NB12.Boilerplate.BuildingBlocks.Application.Ids;
using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Application.Querying;
using NB12.Boilerplate.Modules.Audit.Application.Commands.CleanupInboxProcessed;
using NB12.Boilerplate.Modules.Audit.Application.Commands.DeleteInboxMessage;
using NB12.Boilerplate.Modules.Audit.Application.Commands.ReplayInboxMessage;
using NB12.Boilerplate.Modules.Audit.Application.Commands.RunAuditRetentionCleanup;
using NB12.Boilerplate.Modules.Audit.Application.Enums;
using NB12.Boilerplate.Modules.Audit.Application.Queries.GetAuditRetentionConfig;
using NB12.Boilerplate.Modules.Audit.Application.Queries.GetAuditRetentionStatus;
using NB12.Boilerplate.Modules.Audit.Application.Queries.GetInboxMessages;
using NB12.Boilerplate.Modules.Audit.Application.Queries.GetInboxStats;
using NB12.Boilerplate.Modules.Audit.Application.Security;

namespace NB12.Boilerplate.Modules.Audit.Api.Endpoints
{
    public static class AuditAdminEndpoints
    {
        public static RouteGroupBuilder MapAuditAdminEndpoints(this RouteGroupBuilder group)
        {
            var admin = group.MapGroup("/admin")
                .WithTags("AuditAdmin");

            // Inbox
            admin.MapGet("/inbox/stats", GetInboxStats)
                .RequireAuthorization(AuditPermissions.InboxRead);

            admin.MapGet("/inbox/paged", GetInboxPaged)
                .RequireAuthorization(AuditPermissions.InboxRead);

            admin.MapGet("/inbox/{id}", GetInboxById)
                .RequireAuthorization(AuditPermissions.InboxRead);

            admin.MapPost("/inbox/{id}/replay", ReplayInboxById)
                .RequireAuthorization(AuditPermissions.InboxManage);

            admin.MapDelete("/inbox/{id}", DeleteInboxById)
                .RequireAuthorization(AuditPermissions.InboxManage);

            admin.MapDelete("/inbox/{integrationEventId:guid}/{handlerName}", DeleteInboxEntry)
                .RequireAuthorization(AuditPermissions.InboxManage);

            admin.MapDelete("/inbox/cleanup/processed", CleanupProcessed)
                .RequireAuthorization(AuditPermissions.InboxManage);

            // Retention
            admin.MapGet("/retention", GetRetentionConfig)
                .RequireAuthorization(AuditPermissions.RetentionRead);

            admin.MapGet("/retention/status", GetRetentionStatus)
                .RequireAuthorization(AuditPermissions.RetentionRead);

            admin.MapPost("/retention/run", RunRetention)
                .RequireAuthorization(AuditPermissions.RetentionRun);

            return group;
        }

        private static async Task<IResult> GetInboxStats(ISender sender, HttpContext http, CancellationToken ct)
        {
            var res = await sender.Send(new GetInboxStatsQuery(), ct);
            return res.ToHttpResult(http, x => Results.Ok(x));
        }

        private static async Task<IResult> GetInboxPaged(
            Guid? integrationEventId,
            string? handlerName,
            string? state,
            DateTime? fromUtc,
            DateTime? toUtc,
            string? sortBy,
            int page,
            int pageSize,
            ISender sender,
            HttpContext http,
            CancellationToken ct,
            bool desc = true)
        {
            var parsedState = ParseState(state);

            var res = await sender.Send(new GetInboxMessagesPagedQuery(
                IntegrationEventId: integrationEventId,
                HandlerName: handlerName,
                State: parsedState,
                FromUtc: fromUtc,
                ToUtc: toUtc,
                Page: new PageRequest(page <= 0 ? 1 : page, pageSize <= 0 ? 50 : pageSize),
                Sort: new Sort(sortBy, desc ? SortDirection.Desc : SortDirection.Asc)
            ), ct);

            return res.ToHttpResult(http, x => Results.Ok(x));
        }

        private static async Task<IResult> GetInboxById(
            InboxMessageId id,
            ISender sender,
            HttpContext http,
            CancellationToken ct)
        {
            var res = await sender.Send(new GetInboxMessageQuery(id), ct);
            return res.ToHttpResult(http, x => Results.Ok(x));
        }

        private static async Task<IResult> ReplayInboxById(
            InboxMessageId id,
            ISender sender,
            HttpContext http,
            CancellationToken ct)
        {
            var res = await sender.Send(new ReplayInboxMessageCommand(id), ct);
            return res.ToHttpResult(http);
        }

        private static async Task<IResult> DeleteInboxById(
            InboxMessageId id,
            ISender sender,
            HttpContext http,
            CancellationToken ct)
        {
            var res = await sender.Send(new DeleteInboxMessageByIdCommand(id), ct);
            return res.ToHttpResult(http);
        }

        private static async Task<IResult> DeleteInboxEntry(
            Guid integrationEventId,
            string handlerName,
            ISender sender,
            HttpContext http,
            CancellationToken ct)
        {
            var res = await sender.Send(new DeleteInboxMessageCommand(integrationEventId, handlerName), ct);
            return res.ToHttpResult(http);
        }

        private static async Task<IResult> CleanupProcessed(
            DateTime beforeUtc,
            int maxRows,
            ISender sender,
            HttpContext http,
            CancellationToken ct)
        {
            var res = await sender.Send(new CleanupInboxProcessedCommand(beforeUtc, maxRows), ct);
            return res.ToHttpResult(http, x => Results.Ok(new { deleted = x }));
        }

        private static async Task<IResult> GetRetentionConfig(ISender sender, HttpContext http, CancellationToken ct)
        {
            var res = await sender.Send(new GetAuditRetentionConfigQuery(), ct);
            return res.ToHttpResult(http, x => Results.Ok(x));
        }

        private static async Task<IResult> GetRetentionStatus(ISender sender, HttpContext http, CancellationToken ct)
        {
            var res = await sender.Send(new GetAuditRetentionStatusQuery(), ct);
            return res.ToHttpResult(http, x => Results.Ok(x));
        }

        private static async Task<IResult> RunRetention(ISender sender, HttpContext http, CancellationToken ct)
        {
            var res = await sender.Send(new RunAuditRetentionCleanupCommand(), ct);
            return res.ToHttpResult(http, x => Results.Ok(x));
        }

        private static InboxMessageState ParseState(string? state)
        {
            var s = (state ?? string.Empty).Trim().ToLowerInvariant();

            return s switch
            {
                "pending" => InboxMessageState.Pending,
                "failed" => InboxMessageState.Failed,
                "processed" => InboxMessageState.Processed,
                "deadlettered" or "deadletterd" or "dead-lettered" => InboxMessageState.DeadLettered,
                _ => InboxMessageState.All
            };
        }
    }
}
