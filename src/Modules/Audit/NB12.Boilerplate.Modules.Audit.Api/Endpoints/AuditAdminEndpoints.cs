using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NB12.Boilerplate.BuildingBlocks.Api.ResultHandling;
using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.Modules.Audit.Application.Commands.RunAuditRetentionCleanup;
using NB12.Boilerplate.Modules.Audit.Application.Queries.GetAuditRetentionConfig;
using NB12.Boilerplate.Modules.Audit.Application.Queries.GetAuditRetentionStatus;
using NB12.Boilerplate.Modules.Audit.Application.Security;

namespace NB12.Boilerplate.Modules.Audit.Api.Endpoints
{
    public static class AuditAdminEndpoints
    {
        public static RouteGroupBuilder MapAuditAdminEndpoints(this RouteGroupBuilder group)
        {
            var admin = group.MapGroup("/admin")
                .WithTags("AuditAdmin");


            // Retention
            admin.MapGet("/retention", GetRetentionConfig)
                .RequireAuthorization(AuditPermissions.RetentionRead);

            admin.MapGet("/retention/status", GetRetentionStatus)
                .RequireAuthorization(AuditPermissions.RetentionRead);

            admin.MapPost("/retention/run", RunRetention)
                .RequireAuthorization(AuditPermissions.RetentionRun);

            return group;
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
    }
}
