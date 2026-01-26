using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Audit.Application.Interfaces;

namespace NB12.Boilerplate.Modules.Audit.Application.Commands.DeleteInboxMessage
{
    internal sealed class DeleteInboxMessageCommandHandler
        : IRequestHandler<DeleteInboxMessageCommand, Result>
    {
        private readonly IInboxAdminRepository _repo;

        public DeleteInboxMessageCommandHandler(IInboxAdminRepository repo) => _repo = repo;

        public async Task<Result> Handle(DeleteInboxMessageCommand cmd, CancellationToken ct)
        {
            var ok = await _repo.DeleteAsync(cmd.IntegrationEventId, cmd.HandlerName, ct);
            return ok
                ? Result.Success()
                : Result.Fail(Error.NotFound("audit.inbox.not_found",
                    $"Inbox entry '{cmd.IntegrationEventId}'/'{cmd.HandlerName}' not found."));
        }
    }
}
