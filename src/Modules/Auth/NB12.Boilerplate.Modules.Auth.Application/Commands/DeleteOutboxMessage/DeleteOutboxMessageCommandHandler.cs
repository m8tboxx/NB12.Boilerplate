using NB12.Boilerplate.BuildingBlocks.Application.Enums;
using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Auth.Application.Interfaces;

namespace NB12.Boilerplate.Modules.Auth.Application.Commands.DeleteOutboxMessage
{
    internal sealed class DeleteOutboxMessageCommandHandler
        : IRequestHandler<DeleteOutboxMessageCommand, Result>
    {
        private readonly IOutboxAdminRepository _repo;

        public DeleteOutboxMessageCommandHandler(IOutboxAdminRepository repo) => _repo = repo;

        public async Task<Result> Handle(DeleteOutboxMessageCommand cmd, CancellationToken ct)
        {
            var result = await _repo.DeleteAsync(cmd.Id, ct);

            return result switch
            {
                OutboxAdminWriteResult.Ok => Result.Success(),
                OutboxAdminWriteResult.Locked => Result.Fail(Error.Conflict(
                    "auth.outbox.locked",
                    $"Outbox message '{cmd.Id}' is currently locked and cannot be deleted.")),
                _ => Result.Fail(Error.NotFound(
                    "auth.outbox.not_found",
                    $"Outbox message '{cmd.Id}' not found."))
            };
        }
    }
}
