using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Audit.Application.Enums;
using NB12.Boilerplate.Modules.Audit.Application.Interfaces;

namespace NB12.Boilerplate.Modules.Audit.Application.Commands.DeleteInboxMessage
{
    internal sealed class DeleteInboxMessageByIdCommandHandler
        : IRequestHandler<DeleteInboxMessageByIdCommand, Result>
    {
        private readonly IInboxAdminRepository _repo;

        public DeleteInboxMessageByIdCommandHandler(IInboxAdminRepository repo) => _repo = repo;

        public async Task<Result> Handle(DeleteInboxMessageByIdCommand cmd, CancellationToken ct)
        {
            var result = await _repo.DeleteAsync(cmd.Id, ct);

            return result switch
            {
                InboxAdminWriteResult.Ok => Result.Success(),
                InboxAdminWriteResult.Locked => Result.Fail(Error.Conflict(
                    "audit.inbox.locked",
                    $"Inbox message '{cmd.Id}' is currently locked and cannot be deleted.")),
                _ => Result.Fail(Error.NotFound(
                    "audit.inbox.not_found",
                    $"Inbox message '{cmd.Id}' not found."))
            };
        }
    }
}
