using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NB12.Boilerplate.BuildingBlocks.Api.ResultHandling;
using NB12.Boilerplate.BuildingBlocks.Application.Querying;
using NB12.Boilerplate.Modules.Audit.Application.Queries.GetPagedAuditLogs;
using NB12.Boilerplate.Modules.Audit.Application.Queries.GetPagedErrorLogs;
using NB12.Boilerplate.Modules.Audit.Application.Security;

namespace NB12.Boilerplate.Modules.Audit.Api.Endpoints
{
    public static class AuditReadEndpoints
    {
        public static RouteGroupBuilder MapAuditReadEndpoints(this RouteGroupBuilder group)
        {
            group.MapGet("/auditlogs/paged", GetPagedAuditLogs)
                .RequireAuthorization(AuditPermissions.AuditLogsRead);

            group.MapGet("/errorlogs/paged", GetPagedErrorLogs)
                .RequireAuthorization(AuditPermissions.ErrorLogsRead);

            return group;
        }

        private static async Task<IResult> GetPagedAuditLogs(
            DateTime? fromUtc,
            DateTime? toUtc,
            string? entityType,
            string? entityId,
            string? operation,
            string? userId,
            string? traceId,
            string? sortBy,
            int page,
            int pageSize,
            ISender sender,
            HttpContext http,
            CancellationToken ct,
            bool desc = true)
        {
            var res = await sender.Send(new GetPagedAuditLogsQuery(
                FromUtc: fromUtc,
                ToUtc: toUtc,
                EntityType: entityType,
                EntityId: entityId,
                Operation: operation,
                UserId: userId,
                TraceId: traceId,
                Page: new PageRequest(page <= 0 ? 1 : page, pageSize <= 0 ? 50 : pageSize),
                Sort: new Sort(sortBy, desc ? SortDirection.Desc : SortDirection.Asc)), ct);

            return res.ToHttpResult(http, x => Results.Ok(x));
        }

        private static async Task<IResult> GetPagedErrorLogs(
            DateTime? fromUtc,
            DateTime? toUtc,
            string? userId,
            string? traceId,
            string? sortBy,
            int page,
            int pageSize,
            ISender sender,
            HttpContext http,
            CancellationToken ct,
            bool desc = true)
        {
            var res = await sender.Send(new GetPagedErrorLogsQuery(
                FromUtc: fromUtc,
                ToUtc: toUtc,
                UserId: userId,
                TraceId: traceId,
                Page: new PageRequest(page <= 0 ? 1 : page, pageSize <= 0 ? 50 : pageSize),
                Sort: new Sort(sortBy, desc ? SortDirection.Desc : SortDirection.Asc)), ct);

            return res.ToHttpResult(http, x => Results.Ok(x));
        }
    }
}
