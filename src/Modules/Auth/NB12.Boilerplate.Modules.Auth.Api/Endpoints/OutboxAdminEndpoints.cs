using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NB12.Boilerplate.BuildingBlocks.Api.ResultHandling;
using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Application.Querying;
using NB12.Boilerplate.Modules.Auth.Application.Commands.DeleteOutboxMessage;
using NB12.Boilerplate.Modules.Auth.Application.Commands.ReplayOutboxMessage;
using NB12.Boilerplate.Modules.Auth.Application.Enums;
using NB12.Boilerplate.Modules.Auth.Application.Queries.GetOutboxMessages;
using NB12.Boilerplate.Modules.Auth.Application.Security;

namespace NB12.Boilerplate.Modules.Auth.Api.Endpoints
{
    public static class OutboxAdminEndpoints
    {
        public static RouteGroupBuilder MapOutboxAdminEndpoints(this RouteGroupBuilder group)
        {
            var outbox = group.MapGroup("/outbox").WithTags("Outbox");

            outbox.MapGet("/paged", GetPaged)
                .RequireAuthorization(AuthPermissions.Auth.OutboxRead);

            outbox.MapGet("/{id:guid}", GetById)
                .RequireAuthorization(AuthPermissions.Auth.OutboxRead);

            outbox.MapPost("/{id:guid}/replay", Replay)
                .RequireAuthorization(AuthPermissions.Auth.OutboxReplay);

            outbox.MapDelete("/{id:guid}", Delete)
                .RequireAuthorization(AuthPermissions.Auth.OutboxDelete);

            return group;
        }

        private static async Task<IResult> GetPaged(
            DateTime? fromUtc,
            DateTime? toUtc,
            string? type,
            string? state,
            string? sortBy,
            int page,
            int pageSize,
            ISender sender,
            HttpContext http,
            CancellationToken ct,
            bool desc = true)
        {
            var parsedState = ParseState(state);

            var res = await sender.Send(new GetOutboxMessagesPagedQuery(
                FromUtc: fromUtc,
                ToUtc: toUtc,
                Type: type,
                State: parsedState,
                Page: new PageRequest(page <= 0 ? 1 : page, pageSize <= 0 ? 50 : pageSize),
                Sort: new Sort(sortBy, desc ? SortDirection.Desc : SortDirection.Asc)
            ), ct);

            return res.ToHttpResult(http, x => Results.Ok(x));
        }

        private static async Task<IResult> GetById(
            Guid id,
            ISender sender,
            HttpContext http,
            CancellationToken ct)
        {
            var res = await sender.Send(new GetOutboxMessageQuery(id), ct);
            return res.ToHttpResult(http, x => Results.Ok(x));
        }

        private static async Task<IResult> Replay(
            Guid id,
            ISender sender,
            HttpContext http,
            CancellationToken ct)
        {
            var res = await sender.Send(new ReplayOutboxMessageCommand(id), ct);
            return res.ToHttpResult(http);
        }

        private static async Task<IResult> Delete(
            Guid id,
            ISender sender,
            HttpContext http,
            CancellationToken ct)
        {
            var res = await sender.Send(new DeleteOutboxMessageCommand(id), ct);
            return res.ToHttpResult(http);
        }

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
    }
}
