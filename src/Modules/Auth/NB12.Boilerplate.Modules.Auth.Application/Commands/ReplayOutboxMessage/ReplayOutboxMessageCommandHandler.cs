using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Auth.Application.Interfaces;

namespace NB12.Boilerplate.Modules.Auth.Application.Commands.ReplayOutboxMessage
{
    internal sealed class ReplayOutboxMessageCommandHandler
        : IRequestHandler<ReplayOutboxMessageCommand, Result>
    {
        private readonly IOutboxAdminRepository _repo;

        public ReplayOutboxMessageCommandHandler(IOutboxAdminRepository repo) => _repo = repo;

        public async Task<Result> Handle(ReplayOutboxMessageCommand cmd, CancellationToken ct)
        {
            var ok = await _repo.ReplayAsync(cmd.Id, ct);
            return ok
                ? Result.Success()
                : Result.Fail(Error.NotFound("auth.outbox.not_found", $"Outbox message '{cmd.Id}' not found."));
        }
    }
}
